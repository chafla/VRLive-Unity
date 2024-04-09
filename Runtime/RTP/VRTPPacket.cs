using System;
using System.Buffers.Binary;
using System.IO;
using uOSC;

namespace RTP
{
    public struct VRTPPacket
    {
        public uint PayloadSize;
        public VRTPData OSC;
        public VRTPData Audio;
        public ushort OSCSize => OSC.PayloadSize;
        public ushort AudioSize => Audio.PayloadSize;

        public ushort UserID;

        public byte[] OSCBytes => OSC.Payload;
        public byte[] AudioBytes => Audio.Payload;
        
        public static VRTPPacket FromBuffer(byte[] input)
        {
            var packet = new VRTPPacket();
            var oscStart = 10;
            packet.PayloadSize = BinaryPrimitives.ReadUInt32BigEndian(input[..4]);
            var oscSize = BinaryPrimitives.ReadUInt16BigEndian(input[4..6]);
            var audioSize = BinaryPrimitives.ReadUInt16BigEndian(input[6..8]);
            packet.UserID = BinaryPrimitives.ReadUInt16BigEndian(input[8..oscStart]);
            var oscEndOffset = oscStart + oscSize;
            var oscPayload = input[oscStart..oscEndOffset];
            var audioPayload = input[oscEndOffset..(oscEndOffset + audioSize)];

            packet.Audio = new VRTPData(audioSize, audioPayload, packet.UserID);
            packet.OSC = new VRTPData(oscSize, oscPayload, packet.UserID);
            
            return packet;
        }
    }

    public struct VRTPData
    {
        public ushort PayloadSize;
        public byte[] Payload;
        public ushort UserID;

        public DateTime Arrived;

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
    }
    
   
}