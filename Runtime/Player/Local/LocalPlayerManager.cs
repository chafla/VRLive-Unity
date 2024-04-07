using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using RTP;
using Unity.XR.CoreUtils;
using UnityEngine;
using uOSC;
using VRLive.Runtime.Player.Local;
using VRLive.Runtime.Utils;

namespace VRLive.Runtime.Player
{
    // [RequireComponent(typeof(uOscServer))]
    public class LocalPlayerManager : MonoBehaviour
    {

        /// <summary>
        /// Queue handling mocap that is headed out to the server.
        /// </summary>
        public ConcurrentQueue<Message> mocapOut;

        /// <summary>
        /// The point at which the user will be spawned.
        /// </summary>
        public GameObject spawnPoint;

        /// <summary>
        /// The server that we want to send our mocap to.
        /// For audience members, this should be the audience mocap port.
        /// For performers, it should be the performer mocap port.
        /// </summary>
        // public int localMocapToServerPort;

        /// <summary>
        /// The port that we will be getting mocap in from.
        /// </summary>
        // public int mocapInPort;

        /// <summary>
        /// The IP of the server.
        /// </summary>
        // public string serverHostIP;

        public UserTypeConfig performerConfig;

        public UserTypeConfig audienceConfig;
        
        /// <summary>
        /// The prefab to instantiate as the local user.
        /// </summary>

        public UserType userType;

        public LocalPlayerMotionController localUser;

        private System.Threading.Thread _dispatchThread;

        public OscRelay relay;
        
        public bool hasHandshaked { get; protected set; }

        private bool _hasSpawnedPlayer = false;

        public HMDInputData inputData;

        public XROrigin xrOrigin;

        public void onHandshake(VRLManager manager)
        {
            
            // safe to do here since it's post handshake
            relay.listeningPort = manager.slimeVrMocapInPort;
            relay.destPort = GetTargetMocapPort(manager.remotePorts);
            relay.destIP = manager.hostSettings.remoteIP;
            
            
            relay.StartThreads();
            hasHandshaked = true;

            if (localUser)
            {
                localUser.oscRelay = relay;
                localUser.OnHandshake();
            }
        }

        
        // I feel like I put a good bit of work on this, but it kinda feels like the relay does it better?
        // leaving this here in case it was actually useful
        /*
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
                while (_active && mocapOut != null && mocapOut.TryDequeue(out msg))
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
        */
        
        
        public int GetTargetMocapPort(ServerPortMap map)
        {
            switch (userType)
            {
                case UserType.Audience:
                    return map.audience_mocap_in;
                case UserType.Performer:
                    return map.performer_mocap_in;
                default:
                    Debug.LogWarning("Unknown user type, can't define target mocap port!");
                    return -1;
            }
        }


        public void Awake()
        {
            mocapOut = new ConcurrentQueue<Message>();
            // _active = false;
            relay = gameObject.GetComponent<OscRelay>();
            inputData = gameObject.AddComponent<HMDInputData>();
            relay = gameObject.GetComponent<OscRelay>() ?? gameObject.AddComponent<OscRelay>();
            // relay.listeningPort = mocapInPort;
            // relay.StartThreads();
        }

        public UserTypeConfig GetConfig()
        {
            switch (userType)
            {
                case UserType.Audience:
                    return audienceConfig;
                
                case UserType.Performer:
                    return performerConfig;
                
                default:
                    Debug.LogError("Invalid user type!");
                    return null;
                    
            }
        }

        public virtual void CreatePlayerModel()
        {
            if (_hasSpawnedPlayer)
            {
                Debug.LogWarning("Tried to spawn a player while one already exists!");
                return;
            }

            UserTypeConfig cfg = GetConfig();

            
            var obj = Instantiate(cfg.prefab);

            if (!obj)
            {
                Debug.LogError($"{this} has no prefab defined for {userType}!");
                return;
            }

            
            switch (userType)
            {
                case UserType.Performer:
                    var perfController = obj.GetComponent<LocalPerformerMotionController>() ??
                                         obj.AddComponent<LocalPerformerMotionController>();
                    perfController.oscServer = obj.AddComponent<VRTPOscServer>();
                    localUser = perfController;
                    break;
                
                case UserType.Audience:
                    localUser = obj.GetComponent<LocalAudienceMotionController>() ??
                                obj.AddComponent<LocalAudienceMotionController>();
                    break;
                default:
                    Debug.LogError($"Could not create local player model: invalid user type {userType}!");
                    return;
            }


            localUser.oscRelay = relay;
            localUser.manager = this;
            
            
            
            obj.SetActive(true);
            
            if (cfg.spawnPoint)
            {
                cfg.spawnPoint.MoveTo(obj);
            }

            _hasSpawnedPlayer = true;
        }


        public void OnEnable()
        {
            // oscServer = GetComponent<uOscServer>();
            // oscServer.onDataReceived.AddListener(OnData);

            // _dispatchThread = new System.Threading.Thread(SendMocapDataThread);
            // _active = true;
            // _dispatchThread.Start();
            
            // CreatePlayerModel();
        }
    }

    [Serializable]
    public class UserTypeConfig
    {
        public GameObject prefab;

        public SpawnPoint spawnPoint;
    }
}