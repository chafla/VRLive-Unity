namespace RTP
{
    public static class RtpPacket
    {
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
            rtpPacket[4] = ((byte)((ts >> 24) & 0xFF));
            rtpPacket[5] = ((byte)((ts >> 16) & 0xFF));
            rtpPacket[6] = ((byte)((ts >> 8) & 0xFF));
            rtpPacket[7] = ((byte)((ts >> 0) & 0xFF));
        }

        public static void WriteSSRC(byte[] rtpPacket, uint ssrc)
        {
            rtpPacket[8] = ((byte)((ssrc >> 24) & 0xFF));
            rtpPacket[9] = ((byte)((ssrc >> 16) & 0xFF));
            rtpPacket[10] = ((byte)((ssrc >> 8) & 0xFF));
            rtpPacket[11] = ((byte)((ssrc >> 0) & 0xFF));
        }
    }
}