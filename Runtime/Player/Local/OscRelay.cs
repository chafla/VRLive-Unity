using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using RTP;
using UnityEngine;
using uOSC;
using Thread = System.Threading.Thread;

namespace VRLive.Runtime.Player.Local
{
    public class OscRelay : MonoBehaviour
    {
        public event EventHandler<VRTPData> OnNewMessage;

        public int listeningPort;
        public int destPort;
        public string destIP;

        protected ConcurrentQueue<VRTPData> incomingData;

        public int currentDataPressure;

        private bool _sendActive = false;
        private Thread _sendThread;

        private bool _listenActive = false;
        private Thread _listenThread;

        public Transform rootTransform;

        protected Parser _parser;
        

        /// <summary>
        /// If true, the timestamp of transmission will be marked as the true "mocap timestamp".
        /// This should be a relatively inexpensive operation, as it requires little parsing.
        /// </summary>
        public bool shouldInjectTimestamp = true;

        public void Enqueue(VRTPData data)
        {
            incomingData.Enqueue(data);
        }

        public void Update()
        {
            currentDataPressure = incomingData.Count;
        }
        
        public void Awake()
        {
            incomingData = new ConcurrentQueue<VRTPData>();
            _parser = new Parser();
        }

        public void StartThreads()
        {
            Debug.Log($"Relay listening for incoming local mocap on {listeningPort}");
            Debug.Log($"Relaying mocap onto {destIP}:{destPort}");
            _sendThread = new Thread(SendMocapDataThread);
            _listenThread = new Thread(RecvMocapDataThread);
            if (!_sendActive)
                _sendThread.Start();
            if (!_listenActive)
                _listenThread.Start();
        }

        private Thread _dispatchThread;

        public void RecvMocapDataThread()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.ReceiveTimeout = 5000;
            socket.Blocking = true;
            
            // socket.Bind(new IPEndPoint(new IPAddress(0), listeningPort));
            byte[] buf;
            _listenActive = true;
            var lastPort = listeningPort;
            EndPoint currentEndpoint = new IPEndPoint(new IPAddress(0), listeningPort);
            socket.Bind(currentEndpoint);
            while (_listenActive)
            { 
                if (lastPort != listeningPort)
                {
                    currentEndpoint = new IPEndPoint(new IPAddress(0), listeningPort);
                    lastPort = listeningPort;
                    socket.Bind(currentEndpoint);
                }
                buf = new byte[10000];
                int bytesIn;
                try
                {
                    bytesIn = socket.Receive(buf);
                }
                catch (SocketException e)
                {
                    try
                    {
                        if (e.SocketErrorCode != SocketError.TimedOut)
                        {
                            Debug.LogException(e);
                            // break;
                        }

                        // If we get into a failure loop, don't let it take down our whole application
                        continue;
                    }
                    
                    finally
                    {
                        Thread.Sleep(50);
                    }
                    
                }

                var vrtpData = new VRTPData((ushort) bytesIn, buf[..bytesIn], 0);
                OnNewMessage?.Invoke(this, vrtpData);
                incomingData.Enqueue(vrtpData);
            }
            
        }

        /// <summary>
        /// SlimeVR doesn't transfer HMD info, like the head.
        /// As a result, we need to make sure we pass that along.
        /// This may be a bit expensive, but it's annoyingly kinda necessary if we want to be detached from SteamVR.
        /// </summary>
        // public byte[] InjectRootTransform(VRTPData data)
        // {
        //     int pos = 0;
        //     _parser.Parse(data.Payload, ref pos, data.PayloadSize);
        //     Message msg;
        //     while ((msg = _parser.Dequeue()).address != "")
        //     {
        //         // vmc format from slimevr
        //         if (msg.address == "/VMC/Ext/Root/Pos")
        //         {
        //             
        //         }
        //         else if (msg.address == "/VMC/Ext/Root/Bone")
        //         {
        //             // catch the head bone!
        //         }
        //         // base slimevr osc format
        //         else if (msg.address.StartsWith("/tracking"))
        //         {
        //             
        //         }
        //     }
        // }

        public virtual void ProcessData(ref VRTPData data)
        {
            
        }
        
        public void SendMocapDataThread()
        {
            // TODO find a way to integrate controllers into this as well
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.ReceiveTimeout = 5000;  // to check to see if we're running or not
            _sendActive = true;
            while (_sendActive)
            {
                var endpoint = new IPEndPoint(IPAddress.Parse(destIP), destPort);
                VRTPData data;
                while (incomingData.TryDequeue(out data))
                {
                    if (shouldInjectTimestamp)
                    {
                       data.InjectTimestamp();
                    }
                    ProcessData(ref data);
                    
                    // we don't need to fu
                    
                    // todo if this breaks try sending them individually, but I think bundling them up makes more sense?
                    // Bundle b = new Bundle();
                    socket.SendTo(data.Payload, endpoint);
                    // b.Add(msg);
                    //
                    // if (++messagesInCurBundle >= maxMessagesPerBundle)
                    // {
                    //     stream = new MemoryStream();
                    //     b.Write(stream);
                    //     sock.SendTo(stream.GetBuffer(), currentEndpoint);
                    //     
                    //     messagesInCurBundle = 0;
                    //     b = new Bundle();
                    // }
                }
                
                // 100 tps. Should be plenty to keep up with data coming in?
                Thread.Sleep(5);
            }
        }
    }
}