using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityOpus;

namespace RTP
{
    public class RTPListener : MonoBehaviour
    {
        private Socket socket;
        // public SamplingFrequency sampleRate = SamplingFrequency.Frequency_48000;
        // public NumChannels channels = NumChannels.Mono;
        // public OpusApplication application = OpusApplication.Audio;

        public ConcurrentQueue<VRTPData> AudioDataIn;
        public ConcurrentQueue<VRTPData> MocapDataIn;

        public ushort listeningPort;

        public int mocapPackets = 0;
        public int audioPackets = 0;

        // private ushort _LastPort;

        public bool startOnAwake = false;

        public string label;
        
        protected Decoder Decoder;

        protected Thread NetworkThread;

        [DoNotSerialize]
        public int mocapPacketsQueued;

        [DoNotSerialize]
        public int audioPacketsQueued;

        public int purgeAfterNPacketsPressure = 1000;
        
        public bool Running { get; private set; }

        public event EventHandler<VRTPPacket> OnNewData; 

        private void Awake()
        {
            // _LastPort = ListeningPort;
            AudioDataIn = new ConcurrentQueue<VRTPData>();
            MocapDataIn = new ConcurrentQueue<VRTPData>();

            if (startOnAwake)
            {
                StartServer();
            }
        }

        public void Update()
        {
            mocapPacketsQueued = MocapDataIn.Count;
            audioPacketsQueued = AudioDataIn.Count;

            if (mocapPacketsQueued > purgeAfterNPacketsPressure)
            {
                MocapDataIn.Clear();
            }

            if (audioPacketsQueued > purgeAfterNPacketsPressure)
            {
                AudioDataIn.Clear();
            }
        }


        // private void Update()
        // {
        //     if (_LastPort == ListeningPort) return;
        //     Stop();
        //     StartServer();
        //     _LastPort = ListeningPort;
        // }

        public void StartServer()
        {
            if (Running)
            {
                return;
            }
            Running = true;
            
            NetworkThread = new Thread(NetworkListenerThread);
            NetworkThread.Start();
        }

        public void Stop()
        {
            Running = false;
        }

        private void OnDisable()
        {
            Running = false;
        }

        private void OnDestroy()
        {
            Running = false;
        }

        private void NetworkListenerThread()
        {
            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sock.ReceiveTimeout = 5000;  // every 100ms check to see if we're running or not
            try
            {
                var endpoint = new IPEndPoint(new IPAddress(0), listeningPort);
                sock.Bind(endpoint);
                Debug.Log($"Bound to {label} on {endpoint}");
            }
            catch (SocketException e)
            {
                Debug.LogException(e);
                Debug.LogError($"{label} Failed to bind to {listeningPort}");
                return;
            }
            
            var buffer = new byte[4096];
            int bytesIn;
            VRTPPacket packet;
            int packetsSeen = 0;
            int bytesSeen = 0;
            var lastMessageTime = System.DateTime.Now;
            var messageFrequencyMs = 10000;

            var lastPort = listeningPort;
            EndPoint currentEndpoint = new IPEndPoint(new IPAddress(0), listeningPort);

            EndPoint incomingEndpoint = new IPEndPoint(0, 0);
            
            while (Running)
            {
                buffer = new byte[50000];
                if (lastPort != listeningPort)
                {
                    currentEndpoint = new IPEndPoint(new IPAddress(0), listeningPort);
                    lastPort = listeningPort;
                }
                try
                {
                    // todo come up with a better way to handle the port update so we can bind instead
                    bytesIn = sock.ReceiveFrom(buffer, ref incomingEndpoint);
                }
                catch (SocketException e)
                {
                    if (Running)
                    {
                        if (e.SocketErrorCode != SocketError.TimedOut)
                        {
                            Debug.LogError($"{label} on {listeningPort} had an exception");
                            Debug.LogException(e);
                        }

                        else
                        {
                            Debug.LogError($"Timed out waiting on message from {currentEndpoint}");
                        }
                       
                    }

                    continue;
                }
                
                // Debug.Log("Got new data from RTP listener.");

                bytesSeen += bytesIn;
                packetsSeen++;
                if (bytesIn == 0)
                {
                    Debug.LogError("Got zero bytes from udp socket, may be closed?");
                }
                else
                {
                    var time = System.DateTime.Now;
                    if ((time - lastMessageTime).TotalMilliseconds > messageFrequencyMs)
                    {
                        Debug.Log($"{label} Over last {time - lastMessageTime} on {listeningPort}: {bytesSeen} bytes, {packetsSeen} packets");
                        lastMessageTime = time;
                        packetsSeen = 0;
                        bytesSeen = 0;

                    }
                    
                }

                packet = VRTPPacket.FromBuffer(buffer);

                if (packet.AudioSize > 0)
                {
                    AudioDataIn.Enqueue(packet.Audio);
                    audioPackets++;
                }

                if (packet.OSCSize > 0)
                {
                    MocapDataIn.Enqueue(packet.OSC);
                    mocapPackets++;
                }
                
                OnNewData?.Invoke(this, packet);

            }
        }
    }
}