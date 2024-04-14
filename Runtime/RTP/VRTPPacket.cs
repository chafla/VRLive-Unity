﻿using System;
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

            packet.Audio = new VRTPData(audioSize, audioPayload, packet.UserID);
            packet.OSC = new VRTPData(oscSize, oscPayload, packet.UserID);
            
            return packet;
        }
    }

    public class VRTPData
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

        public static VRTPData FromMessage(Message message, ushort userID)
        {
            // very silly that there's not a common interface between these two but what can ya do
            var stream = new MemoryStream();
            message.Write(stream);
            var buf = stream.GetBuffer();
            var data = new VRTPData((ushort) buf.Length, buf, userID);
            return data;
        }
    }
    
   
}