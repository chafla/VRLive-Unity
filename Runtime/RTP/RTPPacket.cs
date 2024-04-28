using System.Buffers.Binary;

namespace RTP
{
    public static class RtpPacket
    {

        private const ushort BACKING_TRACK_PROF_ID = 5653;
        
        public static void WriteHeader(byte[] rtpPacket
            , int rtpVersion
            , int rtpPadding
            , int rtpExtension
            , int rtpSrcCount
            , int rtpMarker
            , int rtpPayload)
        {
            rtpPacket[0] = (byte)((rtpVersion << 6) | (rtpPadding << 5) | (rtpExtension << 4) | rtpSrcCount);
            rtpPacket[1] = (byte)((rtpMarker << 7) | (rtpPayload & 0x7F));
        }

        public static void WriteSequenceNumber(byte[] rtpPacket, uint emptySeqId)
        {
            rtpPacket[2] = ((byte)((emptySeqId >> 8) & 0xFF));
            rtpPacket[3] = ((byte)((emptySeqId >> 0) & 0xFF));
        }

        public static void WriteTS(byte[] rtpPacket, uint ts)
        {
            // BinaryPrimitives.WriteUInt32LittleEndian(rtpPacket[4..8], ts);
            rtpPacket[4] = ((byte)((ts >> 24) & 0xFF));
            rtpPacket[5] = ((byte)((ts >> 16) & 0xFF));
            rtpPacket[6] = ((byte)((ts >> 8) & 0xFF));
            rtpPacket[7] = ((byte)((ts >> 0) & 0xFF));
        }
        
        public static void WriteTS(byte[] rtpPacket, float ts)
        {
            var floatBytes = System.BitConverter.GetBytes(ts);
            rtpPacket[4] = floatBytes[0];
            rtpPacket[5] = floatBytes[1];
            rtpPacket[6] = floatBytes[2];
            rtpPacket[7] = floatBytes[3];
        }

        public static void WriteSSRC(byte[] rtpPacket, uint ssrc)
        {
            // BinaryPrimitives.WriteUInt32LittleEndian(rtpPacket[8..12], ssrc);
            rtpPacket[8] = ((byte)((ssrc >> 24) & 0xFF));
            rtpPacket[9] = ((byte)((ssrc >> 16) & 0xFF));
            rtpPacket[10] = ((byte)((ssrc >> 8) & 0xFF));
            rtpPacket[11] = ((byte)((ssrc >> 0) & 0xFF));
        }

        public static void WriteBackingTrackPositionField(byte[] rtpPacket, uint backingTrackPos)
        {
            rtpPacket[12] = ((byte)((BACKING_TRACK_PROF_ID >> 8) & 0xFF));
            rtpPacket[13] = ((byte)((BACKING_TRACK_PROF_ID >> 0) & 0xFF));
            // profile ID, this is app-specific but we'll just set it to something special here
            BinaryPrimitives.WriteUInt16BigEndian(rtpPacket[12..14], BACKING_TRACK_PROF_ID);
            // The size in 32-bit units of our header payload -- one u32 is all we need
            BinaryPrimitives.WriteUInt16BigEndian(rtpPacket[14..16], 1);
            rtpPacket[14] = 0;
            rtpPacket[15] = ((byte)((1) & 0xFF));
            
            // var floatBytes = 
            
            rtpPacket[16] = ((byte)((backingTrackPos >> 24) & 0xFF));
            rtpPacket[17] = ((byte)((backingTrackPos >> 16) & 0xFF));
            rtpPacket[18] = ((byte)((backingTrackPos >> 8) & 0xFF));
            rtpPacket[19] = ((byte)((backingTrackPos >> 0) & 0xFF));
            // BinaryPrimitives.WriteUInt32BigEndian(rtpPacket[16..20], backingTrackPos);

        }
        
        
        public static void WriteBackingTrackPositionField(byte[] rtpPacket, float backingTrackPos)
        {
            rtpPacket[12] = ((byte)((BACKING_TRACK_PROF_ID >> 8) & 0xFF));
            rtpPacket[13] = ((byte)((BACKING_TRACK_PROF_ID >> 0) & 0xFF));
            // profile ID, this is app-specific but we'll just set it to something special here
            BinaryPrimitives.WriteUInt16BigEndian(rtpPacket[12..14], BACKING_TRACK_PROF_ID);
            // The size in 32-bit units of our header payload -- one u32 is all we need
            BinaryPrimitives.WriteUInt16BigEndian(rtpPacket[14..16], 1);
            rtpPacket[14] = 0;
            rtpPacket[15] = ((byte)((1) & 0xFF));

            var floatBytes = System.BitConverter.GetBytes(backingTrackPos);
            
            rtpPacket[16] = floatBytes[0];
            rtpPacket[17] = floatBytes[1];
            rtpPacket[18] = floatBytes[2];
            rtpPacket[19] = floatBytes[3];
            // BinaryPrimitives.WriteUInt32BigEndian(rtpPacket[16..20], backingTrackPos);

        }
    }
}