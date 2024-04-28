using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using UnityEngine;
using uOSC;

namespace RTP
{
    public struct VRTPPacket
    {
        public uint PayloadSize;
        public VRTPDataInternal OSC;
        public VRTPDataInternal Audio;
        public ushort OSCSize => OSC.PayloadSize;
        public ushort AudioSize => Audio.PayloadSize;

        public ushort UserID;

        public float backingTrackPosition;

        public byte[] OSCBytes => OSC.Payload;
        public byte[] AudioBytes => Audio.Payload;
        
        public static VRTPPacket FromBuffer(byte[] input)
        {
            var packet = new VRTPPacket();
            var oscStart = 14;
            packet.PayloadSize = BinaryPrimitives.ReadUInt32BigEndian(input[..4]);
            var oscSize = BinaryPrimitives.ReadUInt16BigEndian(input[4..6]);
            var audioSize = BinaryPrimitives.ReadUInt16BigEndian(input[6..8]);
            packet.UserID = BinaryPrimitives.ReadUInt16BigEndian(input[8..10]);
            var res = input[10..oscStart];
            Array.Reverse(res);
            packet.backingTrackPosition = System.BitConverter.ToSingle(res);
            // packet.backingTrackPosition = BinaryPrimitives.(input[10..oscStart]);
            var oscEndOffset = oscStart + oscSize;
            var oscPayload = input[oscStart..oscEndOffset];
            var audioPayload = input[oscEndOffset..(oscEndOffset + audioSize)];

            packet.Audio = new VRTPDataInternal(audioSize, audioPayload, packet.UserID, packet.backingTrackPosition);
            packet.OSC = new VRTPDataInternal(oscSize, oscPayload, packet.UserID, packet.backingTrackPosition);
            
            return packet;
        }
    }

    public class VRTPData
    {
        private static byte[] _bundleIntro = Encoding.UTF8.GetBytes("#bundle");

        public readonly ushort PayloadSize;
        public readonly byte[] Payload;
        public readonly ushort UserID;
        
        public readonly DateTime Arrived;

        public VRTPData(ushort size, byte[] data, ushort userID)
        {
            PayloadSize = size;
            Payload = data;
            UserID = userID;
            Arrived = DateTime.Now;
        }

        public static VRTPData FromBundle(Bundle bundle, ushort userId)
        {
            var stream = new MemoryStream();
            bundle.Write(stream);
            var buf = stream.GetBuffer();
            var data = new VRTPData((ushort) buf.Length, buf, userId);
            return data;
        }

        public virtual float GetBackingTrackPosition()
        {
            // the base doesn't keep track of this
            return -1;
        }

        public void InjectTimestamp()
        {
            // how far to look for our bundle before giving up
            var maxBytesToSearch = 50;
            // var noTimestamp = true;
            var noInnerTimestamp = false;
            for (int i = 0; i < PayloadSize - _bundleIntro.Length && i < maxBytesToSearch; i++)
            {
                // check linearly to find #bundle
                for (int j = 0; j < _bundleIntro.Length; j++)
                {
                    if (Payload[i + j] != _bundleIntro[j])
                    {
                        noInnerTimestamp = true;
                        break;
                    }
                }

                if (noInnerTimestamp)
                {
                    continue;
                }


                // get the starting index of our timetag
                // it starts one character after the bundle intro
                var timetagPos = i + _bundleIntro.Length + 1;
                // 64 big-endian fixed point time tag
                // first 32 bits are for the epoch seconds
                // last 32 bits are for fractional seconds (2<<32 would technically be 1.0)

                // var curTime = DateTime.Now;


                // https://stackoverflow.com/a/21055459
                // this method gets us fractional seconds as well
                // also note that according to the spec it's time since 1/1/1900
                var timeSpan = DateTime.UtcNow - new DateTime(1900, 1, 1, 0, 0, 0);
                var timeSeconds = timeSpan.TotalSeconds;
                var timeSecsTrunc = (uint)timeSeconds; // this may lose some precision after 2038 be warned
                var fracSecs = timeSeconds - timeSecsTrunc;
                var fracSecsTotal = fracSecs * (100000000);

                var fracSecsBytes = BitConverter.GetBytes((uint)fracSecsTotal);
                var totalSecsBytes = BitConverter.GetBytes(timeSecsTrunc);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(fracSecsBytes);
                    Array.Reverse(totalSecsBytes);
                }

                Array.Copy(totalSecsBytes, 0, Payload, timetagPos, totalSecsBytes.Length);
                Array.Copy(fracSecsBytes, 0, Payload, timetagPos + 4, fracSecsBytes.Length);
                return;


            }

            Debug.LogWarning("Could not find a #bundle tag in parsed OSC message.");

        }

        public DateTime? ExtractTimestamp()
        {
            var baseTime = new DateTime(1900, 1, 1, 0, 0, 0);
            // how far to look for our bundle before giving up
            var maxBytesToSearch = 50;
            // var noTimestamp = true;
            var noInnerTimestamp = false;
            for (int i = 0; i < PayloadSize - _bundleIntro.Length && i < maxBytesToSearch; i++)
            {
                // check linearly to find #bundle
                for (int j = 0; j < _bundleIntro.Length; j++)
                {
                    if (Payload[i + j] != _bundleIntro[j])
                    {
                        noInnerTimestamp = true;
                        break;
                    }
                }

                if (noInnerTimestamp)
                {
                    continue;
                }


                // get the starting index of our timetag
                // it starts one character after the bundle intro
                var timetagPos = i + _bundleIntro.Length + 1;
                
                // var fracSecs = BitConverter.ToUInt32(Payload, timetagPos);
                // var fullSecs = BitConverter.ToUInt32(Payload, timetagPos + 4);
                var fullSecs = BinaryPrimitives.ReadUInt32BigEndian(Payload[timetagPos..]);
                var fracSecs = BinaryPrimitives.ReadUInt32BigEndian(Payload[(timetagPos + 4)..]);

                double fractionalSecs = fracSecs;
                while (fractionalSecs > 1)
                {
                    fractionalSecs /= 10;
                }
                // 64 big-endian fixed point time tag
                // first 32 bits are for the epoch seconds
                // last 32 bits are for fractional seconds (2<<32 would technically be 1.0)
                
                // this method gets us fractional seconds as well
                // also note that according to the spec it's time since 1/1/1900
                
                var timestamp = baseTime.AddSeconds(fullSecs + fractionalSecs);

                return timestamp;


            }

            Debug.LogWarning("Could not find a #bundle tag in parsed OSC message.");

            return null;

        }

    }

    public class VRTPDataInternal : VRTPData
    {
        private readonly float _backingTrackPosition;
        public VRTPDataInternal(ushort size, byte[] data, ushort userID, float backingTrackPosition) : base(size, data, userID)
        {
            _backingTrackPosition = backingTrackPosition;
        }

        public override float GetBackingTrackPosition()
        {
            return _backingTrackPosition;
        }
    }
    
   
}