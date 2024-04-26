using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityOpus;
using VRLive.Runtime;


// please forgive this file being a little messy
// this is kind of a side-effect of it being hacked together from a few different tutorials on how to get RTP audio
// working in unity (the truth: it's tricky)

namespace RTP
{
    

    public class AudioStreamer : MonoBehaviour
    {
        /// <summary>
        /// The base length of the RTP header field, not accounting for extensions
        /// </summary>
        private const int RTP_HEADER_LEN = 12;

        [DoNotSerialize]
        private bool _sendBackingTrackData = true;

        /// <summary>
        /// How long the extension field of the rtp header is
        /// </summary>
        private const int RTP_EXTENSION_LEN = 8;
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

        // This may be seen by some as a little hacky but it's kind of the best bet we got here
        // Smuggle the time in samples of the backing track along the start of the audio track.
        // We have a lot of overhead here, we can usually spare another four bytes.
        
        // This is a best-effort thing. If we can't easily find the backing track listener, then we just don't send it.
        // Easy as that.
        
        public BackingTrackManager backingTrackManager;

        public float lastBackingTrackPosition = 0;

        public bool Verbose = false;

        // Must be one of 2.5, 5, 10, 20, 40, or 60
        public int packetDurMs = 20;

        private double desiredFrameDuration => packetDurMs / 1000.0;

        private bool _active = false;

        private ConcurrentQueue<float[]> _rawAudioDataFromMic;

        private Thread _encodeThread;

        /// <summary>
        /// The desired frame size given the sample rate.
        /// Note that "this must be an Opus frame size for the encoder's sampling rate. For example, at 48 kHz the permitted values are 120, 240, 480, 960, 1920, and 2880."
        /// see: https://www.opus-codec.org/docs/opus_api-1.2/group__opus__encoder.html
        /// </summary>
        private int desiredFrameSize;
        
        // Opus encoder
        protected Encoder Encoder;


        private void Awake()
        {
            _rawAudioDataFromMic = new ConcurrentQueue<float[]>();
            backingTrackManager = gameObject.GetComponent<BackingTrackManager>();
        }

        private void OnDisable()
        {
            _active = false;
        }

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
                , _sendBackingTrackData ? 1 : 0  // extension is 1: we are including a header extension to note the location of our backing track
                , 0 // csrc_count
                , 1 // marker, set to one for last packet
                , 96); // payload_type PCM 16bits BE signed
            RtpPacket.WriteSequenceNumber(rtpPacket, sequenecId);
            RtpPacket.WriteTS(rtpPacket, Convert.ToUInt32(DateTime.Now.Millisecond * 90));
            RtpPacket.WriteSSRC(rtpPacket, 0);
            // our addition
            // if (backingTrackManager)
            // {
            //     
            // }
            if (_sendBackingTrackData)
                RtpPacket.WriteBackingTrackPositionField(rtpPacket, lastBackingTrackPosition);
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
            // int maxEthMTU = 1400;
            int offset = 0;
            // while (dataToSend > 0)
            // {
                // var bodyLen = Math.Min(dataToSend, maxEthMTU);
                var headerLen = RTP_HEADER_LEN + (_sendBackingTrackData ? RTP_EXTENSION_LEN : 0);
                var rtpAudioData = new byte[headerLen + length];
                SetRtpHeader(rtpAudioData);
                Array.Copy(byteArray, offset, rtpAudioData, headerLen, length);
                IPEndPoint remoteEndPoint;
                try
                {
                    remoteEndPoint = new IPEndPoint(IPAddress.Parse(sendServerIP), sendServerPort);
                }
                catch (FormatException)
                {
                    Debug.LogWarning($"Invalid network address with IP: {sendServerIP} and port {sendServerPort}");
                    return;
                }

                int dataSent = socket.SendTo(rtpAudioData, 0, rtpAudioData.Length, SocketFlags.None, remoteEndPoint);
                // dataToSend = dataToSend - dataSent;
                // offset = offset + dataSent;
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

            _encodeThread = new Thread(EncodeAndSendThread);
            _active = true;
            _encodeThread.Start();
        }

        public void EncodeAndSendThread()
        {
            float[] dataOut;

            // data going into the encoder
            float[] encoderBuf;
            
            // and data coming out 

            float[] encOut = new float[desiredFrameSize];

            while (_active)
            {
                while (_rawAudioDataFromMic.TryDequeue(out dataOut))
                {
                    
                    int framePointer = 0;

                    while (framePointer < dataOut.Length)
                    {
                        encoderBuf = new float[desiredFrameSize];
                        var bytesSafe = dataOut.Length - (framePointer + desiredFrameSize);
                        int bytesCopied = Math.Min(dataOut.Length - framePointer, desiredFrameSize);
                        Array.Copy(dataOut, framePointer, encoderBuf, 0, bytesCopied);

                        framePointer += bytesCopied;

                        byte[] output;
                        
                        try
                        {
                            output = new byte[dataOut.Length];
                        }
                        catch (OverflowException e)
                        {
                            Debug.LogError($"Failed to read samples: pos - last pos = {pos - lastPos}, len = {encoderBuf.Length}");
                            Debug.LogException(e);
                            continue;
                        }

                        
                        var res = Encoder.Encode(encoderBuf, output);
                        if (res < 0)
                        {
                            Debug.LogError($"Opus encoder error: {res}");
                        }

                        // if (backingTrackManager != null)
                        // {
                        //        
                        //     Array.Copy();
                        // }

                        SendToServer(output, res);
                    }
                    // encoder returns either a positive value indicating the number of bytes encoded,
                    // or a negative value indicating the error code
                    
                    // To encode a frame, opus_encode() or opus_encode_float() must be called with exactly one frame (2.5, 5, 10, 20, 40 or 60 ms) of audio data:
                    // (20ms is standard)
                    // https://www.opus-codec.org/docs/opus_api-1.2/group__opus__encoder.html
                    
                    
                }
            }
            
            // we want this tight but since we should be getting 20ms of audio we don't want this to fall behind
            Thread.Sleep(1);
        }

        private void Update()
        {
            if (backingTrackManager)
            {
                lastBackingTrackPosition = backingTrackManager.source.time;
            }
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
                    // var bytesPerSample = sizeof(float);
                    // var effectiveFrameSize = (pos - lastPos) / bytesPerSample / mic.channels;
                    
                    // Allocate the space for the new sample.
                    int len = readLength * mic.channels;
                    float[] samples = new float[len];
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
                    
                    _rawAudioDataFromMic.Enqueue(samples);
                    
                    
                    lastPos = pos;
                    
                    
                    

                    
                   
                    
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