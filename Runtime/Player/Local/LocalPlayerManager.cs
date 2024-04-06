using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using uOSC;
using VRLive.Runtime.Player.Local;

namespace VRLive.Runtime.Player
{
    // [RequireComponent(typeof(uOscServer))]
    public class LocalPlayerManager : MonoBehaviour
    {
        /// <summary>
        /// Use the proper OSC server for what the player will be seeing/displayed as.
        /// This, and by association its port, is where mocap should be sent to the client.
        /// </summary>
        // public uOscServer oscServer;

        /// <summary>
        /// Queue handling mocap that is headed out to the server.
        /// </summary>
        public ConcurrentQueue<Message> mocapOut;

        /// <summary>
        /// The server that we want to send our mocap to.
        /// For audience members, this should be the audience mocap port.
        /// For performers, it should be the performer mocap port.
        /// </summary>
        public int localMocapToServerPort;

        public int mocapInPort;

        /// <summary>
        /// The IP of the server.
        /// </summary>
        public string serverHostIP;

        /// <summary>
        /// The controller managing the motion of the local player.
        /// </summary>
        public LocalPlayerController controller;
        
        /// <summary>
        /// The prefab to instantiate as the local user.
        /// </summary>
        public GameObject localUserPrefab;

        private System.Threading.Thread _dispatchThread;

        public OscRelay relay;

        private bool _active = false;

        public void onHandshake()
        {
            relay.StartThreads();
        }

        public virtual void OnData(Message msg)
        {
            mocapOut.Enqueue(msg);
            // print(msg);
        }
        
        
        
        public void SendMocapDataThread()
        {
            // TODO find a way to integrate controllers into this as well
            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sock.ReceiveTimeout = 5000;  // to check to see if we're running or not
            
            // try
            // {
            //     sock.Connect(new IPEndPoint(IPAddress.Parse(serverHostIP), localMocapToServerPort));
            // }
            // catch (SocketException e)
            // {
            //     Debug.LogException(e);
            //     Debug.LogError($"Local player manager failed to connect to {localMocapToServerPort}");
            //     return;
            // }
            var lastPort = localMocapToServerPort;
            var lastHost = serverHostIP;
            EndPoint currentEndpoint = new IPEndPoint(IPAddress.Parse(serverHostIP), localMocapToServerPort);
            

            var buffer = new byte[4096];
            MemoryStream stream;

            while (_active)
            {
                if (lastPort != localMocapToServerPort || lastHost != serverHostIP)
                {
                    currentEndpoint = new IPEndPoint(IPAddress.Parse(serverHostIP), localMocapToServerPort);
                    lastPort = localMocapToServerPort;
                    lastHost = serverHostIP;
                }

                Bundle b = new Bundle();
                Message msg;
                // we have to be careful about this: slimeVR is capable of outputting a LOT of data.
                // This can lead to significant pressure, not on the rust side but on the unity side, trying to keep up with everything.
                var messagesInCurBundle = 0;
                var maxMessagesPerBundle = 10;
                while (_active && mocapOut.TryDequeue(out msg))
                {
                    // todo if this breaks try sending them individually, but I think bundling them up makes more sense?
                    // Bundle b = new Bundle();
                    b.Add(msg);
                    
                    if (++messagesInCurBundle >= maxMessagesPerBundle)
                    {
                        stream = new MemoryStream();
                        b.Write(stream);
                        sock.SendTo(stream.GetBuffer(), currentEndpoint);
                        
                        messagesInCurBundle = 0;
                        b = new Bundle();
                    }
                }
            }
        }
        
        
        public virtual int GetTargetMocapPort(ServerPortMap map)
        {
            // TODO OVERRIDE THIS
            return map.audience_mocap_in;
        }


        public void Awake()
        {
            mocapOut = new ConcurrentQueue<Message>();
            // _active = false;
            relay = GetComponent<OscRelay>() ?? gameObject.AddComponent<OscRelay>();
            // relay.listeningPort = mocapInPort;
            // relay.StartThreads();
        }

        public void OnDisable()
        {
            _active = false;
        }

        public void OnEnable()
        {
            // oscServer = GetComponent<uOscServer>();
            // oscServer.onDataReceived.AddListener(OnData);

            _dispatchThread = new System.Threading.Thread(SendMocapDataThread);
            _active = true;
            _dispatchThread.Start();
        }
    }
}