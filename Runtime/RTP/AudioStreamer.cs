using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityOpus;


namespace RTP
{
    

    public class AudioStreamer : MonoBehaviour
    {
        const int RTP_HEADER_LEN = 12;
        // Audio control variables
        AudioClip mic;
        int lastPos, pos;

        // UDP Socket variables
        private Socket socket;
        private UInt32 sequenecId = 0;

        // public String ServerIP
        // {
        //     get => RemoteEndPoint.Address.ToString();
        //     set => RemoteEndPoint = new IPEndPoint(IPAddress.Parse(value), ServerPort);
        // }
        //
        // public int ServerPort
        // {
        //     get => RemoteEndPoint.Port;
        //     set => RemoteEndPoint = new IPEndPoint(IPAddress.Parse(ServerIP), value);
        // }
        
        public String sendServerIP;
        public int sendServerPort;
        
        // opus encoder variables
        public SamplingFrequency sampleRate = SamplingFrequency.Frequency_48000;
        public NumChannels channels = NumChannels.Mono;
        public OpusApplication application = OpusApplication.Audio;

        public bool Verbose = false;

        // Must be one of 2.5, 5, 10, 20, 40, or 60
        public int packetDurMs = 20;

        private double desiredFrameDuration => packetDurMs / 1000.0;

        /// <summary>
        /// The desired frame size given the sample rate.
        /// Note that "this must be an Opus frame size for the encoder's sampling rate. For example, at 48 kHz the permitted values are 120, 240, 480, 960, 1920, and 2880."
        /// see: https://www.opus-codec.org/docs/opus_api-1.2/group__opus__encoder.html
        /// </summary>
        private int desiredFrameSize;
        
        // Opus encoder
        protected Encoder Encoder;
        
        

        void SetRtpHeader(byte[] rtpPacket)
        {
            // Populate RTP Packet Header
            // 0  - Version, P, X, CC, M, PT and Sequence Number
            // 32 - Timestamp. H264 uses a 90kHz clock
            // 64 - SSRC
            // 96 - CSRCs (optional)
            // nn - Extension ID and Length
            // nn - Extension header
            RtpPacket.WriteHeader(rtpPacket
                , 2 // version
                , 0 // padding
                , 0 // extension
                , 0 // csrc_count
                , 1 // marker, set to one for last packet
                , 96); // payload_type PCM 16bits BE signed
            RtpPacket.WriteSequenceNumber(rtpPacket, sequenecId);
            RtpPacket.WriteTS(rtpPacket, Convert.ToUInt32(DateTime.Now.Millisecond * 90));
            RtpPacket.WriteSSRC(rtpPacket, 0);
            sequenecId++;
        }
        
        
        void SendToServer(byte[] samples, int length)
        {
            SendData(samples, length);
        }

        void SendToServer(float[] samples)
        {
            // Convert audio from float to signed 16 bit PCM BigEndian and copy it to the byte array
            var byteArray = new byte[samples.Length * sizeof(Int16)]; // to convert each sample float to Int16
            int i = 0;
            int j = 0;
            while (i < samples.Length)
            {
                Int16 sample = Convert.ToInt16((samples[i] * Int16.MaxValue) / 100);
                byteArray[j] = (byte)(sample & 0xFF);
                byteArray[j + 1] = (byte)((sample >> 8) & 0xFF);
                i += 1;
                j += 2;
            }
            
            SendData(byteArray, byteArray.Length);

        }

        void SendData(byte[] byteArray, int length)
        {
            if (socket == null) return;
            // if (samples == null || samples.Length == 0) return;
            var dataToSend = length;
            // int maxEthMTU = 1400;
            int offset = 0;
            // while (dataToSend > 0)
            // {
                // var bodyLen = Math.Min(dataToSend, maxEthMTU);
                var bodyLen = dataToSend;  // who cares about mtu tbh
                var rtpAudioData = new byte[RTP_HEADER_LEN + bodyLen];
                SetRtpHeader(rtpAudioData);
                Array.Copy(byteArray, offset, rtpAudioData, RTP_HEADER_LEN, bodyLen);
                IPEndPoint remoteEndPoint;
                try
                {
                    remoteEndPoint = new IPEndPoint(IPAddress.Parse(sendServerIP), sendServerPort);
                }
                catch (FormatException e)
                {
                    Debug.LogWarning($"Invalid network address with IP: {sendServerIP} and port {sendServerPort}");
                    return;
                }

                int dataSent = socket.SendTo(rtpAudioData, 0, rtpAudioData.Length, SocketFlags.None, remoteEndPoint);
                dataToSend = dataToSend - dataSent;
                offset = offset + dataSent;
            // }
        }

        void Start()
        {
            // RemoteEndPoint = new IPEndPoint(IPAddress.Parse(sendServerIP), sendServerPort);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            Encoder = new Encoder(sampleRate, channels, application);

            desiredFrameSize = (int)(desiredFrameDuration * (int)sampleRate);
            
            // want length to be as small as possible
            mic = Microphone.Start(null, true, 1,  (int) sampleRate); // Mono
            
        }

        private void Update()
        {
            if ((pos = Microphone.GetPosition(null)) > 0)
            {
                

                if (pos - lastPos < 0)
                {
                    lastPos = 0;
                }
                var readLength = pos - lastPos;
                // if (readLength < 0)
                // {
                //     readLength = pos + lastPos;
                //     // sampleWidth = 
                // }
                // var sampleWidth = pos - lastPos;
                // var sampleWidth = 1920;

                if (pos - lastPos > 0)
                {
                    var bytesPerSample = sizeof(float);
                    // var effectiveFrameSize = (pos - lastPos) / bytesPerSample / mic.channels;
                    
                    // Allocate the space for the new sample.
                    int len = readLength * mic.channels;
                    float[] samples = new float[desiredFrameSize];
                    byte[] output;
                    try
                    {
                        output = new byte[len];
                    }
                    catch (OverflowException e)
                    {
                        Debug.LogError($"Failed to read samples: pos - last pos = {pos - lastPos}, len = {len}");
                        Debug.LogException(e);
                        return;
                    }

                    if(Verbose)
                        print($"{len} samples, consuming up to {desiredFrameSize}");
                    mic.GetData(samples, lastPos);
                    
                    try
                    {
                        // encoder returns either a positive value indicating the number of bytes encoded,
                        // or a negative value indicating the error code
                        
                        // To encode a frame, opus_encode() or opus_encode_float() must be called with exactly one frame (2.5, 5, 10, 20, 40 or 60 ms) of audio data:
                        // (20ms is standard)
                        // https://www.opus-codec.org/docs/opus_api-1.2/group__opus__encoder.html
                        var res = Encoder.Encode(samples, output);
                        if (res < 0)
                        {
                            Debug.LogError($"Bad error code: {res}");
                        }

                        SendToServer(output, res);
                    }
                    finally
                    {
                        lastPos = pos;
                    }
                    
                    

                    
                   
                    
                }
                else if (readLength < 0)
                {
                    Debug.LogWarning($"We got a negative length of {readLength}?");
                }
            }
        }

        void OnDestroy()
        {
            Microphone.End(null);
        }
    }
}