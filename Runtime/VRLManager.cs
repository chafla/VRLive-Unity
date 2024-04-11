using System;
using System.ComponentModel;
using System.Net;
using JetBrains.Annotations;
using RTP;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Management;
using VRLive.Runtime.Player;
using VRLive.Runtime.Player.Local;

namespace VRLive.Runtime
{
    /// <summary>
    /// The overall manager for someone interacting with a VRL server.
    /// Each scene should have one of these -- this fundamentally represents the "client" in our architecture.
    /// </summary>
    // [RequireComponent(typeof(VRTPOscServer))]
    public class VRLManager : MonoBehaviour
    {
        
        // we're using uniJSON just because it's already packed in
        public HandshakeManager HandshakeManager;

        /// <summary>
        /// The name for the client, as presented to the server. Should ideally be unique, though it doesn't have to be.
        /// </summary>
        public string clientIdentifier;

        [FormerlySerializedAs("userType")] public UserType localUserType;

        public ushort clientUserId;

        public ClientPortMap localPorts;

        // [ReadOnly(true)]
        public ServerPortMap remotePorts;

        public int slimeVrMocapInPort;

        public HostSettings hostSettings;

        /// <summary>
        /// If true, don't try to spin up audience or performer parts.
        /// </summary>
        public bool onlyRunLocal;
        
        #region Components
        // all of the different components we expect to have at runtime

        public BackingTrackManager backingTrackManager;

        public ServerEventManager serverEventManager;

        public AudioStreamer audioStreamer;

        public RTPListener rtpListener;

        public RTPAudioListenerComponentized rtpAudioListener;
        
        public VRTPOscServer oscServer;

        public AudienceManager audienceManager;

        public PerformerManager performerManager;

        // public Motion remotePlayerController;

        public LocalPlayerManager localPlayerManager;
        
        #endregion

        private bool _consumedHandshake = false;

        [CanBeNull] private HandshakeResult _handshakeResult;

        public GameObject baseAudienceObjectType;

        public GameObject basePerformerObjectType;

        // Event invoked after a handshake has been successfully handled, meaning that all of the constituent items
        // should be initialized.
        // public event EventHandler<HandshakeCompletion> AfterConsumeHandshake; 

        public void Awake()
        {
            HandshakeManager = new HandshakeManager(hostSettings.HandshakeEndPoint(), localPorts, localUserType, clientIdentifier);
            HandshakeManager.OnHandshakeCompletion += OnHandshakeSuccessEvent;
            HandshakeManager.RunHandshake();
            
            // https://forum.unity.com/threads/manual-openxr-load-with-unity-input-system-not-working.1075966/
            // force restart the xr manager, it doesn't always get shut down properly
            if( XRGeneralSettings.Instance.Manager.activeLoader != null ){
                XRGeneralSettings.Instance.Manager.StopSubsystems();
                XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            }
            XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
            XRGeneralSettings.Instance.Manager.StartSubsystems();
            
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Contains("--audience"))
                {
                    localUserType = UserType.Audience;
                    Debug.LogWarning("Starting up as audience member as per command line args!");
                } 
                else if (args[i].Contains("--performer"))
                {
                    localUserType = UserType.Performer;
                    Debug.LogWarning("Starting up as performer as per command line args!");
                }
                // else if (args[i].Contains("-debugMode"))
                // {
                //     GameProperties.DebugMode = true;
                // }
            }
            
        }

        // note: this is called by a function in a thread, so it can't interact with unity and instead needs to be called
        // on update.
        public void OnHandshakeSuccessEvent(object src, HandshakeResult result)
        {
            _handshakeResult = result;
            _consumedHandshake = false;
        }

        /// <summary>
        /// Actually handle the handshake result.
        /// This is invoked within Update() rather than an external thread, and so it can actually interact with Unity.
        /// </summary>
        /// <param name="result"></param>
        private void HandleHandshakeCompletion(HandshakeResult result)
        {
            clientUserId = result.userId;
            remotePorts = result.serverPorts;
            
            backingTrackManager ??= gameObject.AddComponent<BackingTrackManager>();
            // TODO clean this up in a way that doesn't involve as much duplication, maybe by passing some value in
            backingTrackManager.Listener.Port = result.serverPorts.backing_track_conn_port;
            backingTrackManager.Listener.Host = hostSettings.remoteIP;
            backingTrackManager.Listener.label = "backing track";
            backingTrackManager.Listener.StartListener();
            serverEventManager ??= gameObject.AddComponent<ServerEventManager>();
            serverEventManager.Listener.Port = result.serverPorts.server_event_conn_port;
            serverEventManager.Listener.Host = hostSettings.remoteIP;
            serverEventManager.Listener.label = "server event";
            serverEventManager.Listener.StartListener();
            
            if (localUserType == UserType.Performer)
            {
                // this one's a bit more dynamic
                audioStreamer ??= gameObject.AddComponent<AudioStreamer>();
                audioStreamer.sendServerPort = result.serverPorts.performer_audio_in;
                audioStreamer.sendServerIP = hostSettings.remoteIP;
                // audioStreamer.s
            }

            if (onlyRunLocal)
            {
                rtpListener ??= gameObject.GetComponent<RTPListener>() ?? gameObject.AddComponent<RTPListener>();
                rtpListener.listeningPort = localPorts.vrtp_data;
                rtpListener.label = "VRL Manager";
                rtpListener.StartServer();

                // these both rely on the rtp listener
                oscServer ??= gameObject.AddComponent<VRTPOscServer>();
                // oscServer.StartServer();
                rtpAudioListener ??= gameObject.AddComponent<RTPAudioListenerComponentized>();
                rtpAudioListener.Listener = rtpListener;
                return;
            }
            
            SpawnAudienceHandler();
            
            SpawnPerformerHandler();
            
            SpawnLocalPlayerHandler();

            // create the remaining players.
            
            foreach (var user in result.OtherUsers)
            {
                switch (user.user_type)
                {
                    case UserType.Audience:
                        if (!audienceManager)
                        {
                            Debug.LogWarning("No audience manager exists, cannot create related user!");
                            break;
                        }
                        audienceManager.CreateNewPlayer(user.user_id, user.user_type);
                        break;
                    case UserType.Performer:
                        if (!performerManager)
                        {
                            Debug.LogWarning("No performer manager exists, cannot create related user!");
                            break;
                        }
                        performerManager.CreateNewPlayer(user.user_id, user.user_type);
                        break;
                    default:
                        Debug.LogWarning("Got an invalid user type when trying to add other users!");
                        break;
                }
               
            }
            
            
        }

        public void SpawnLocalPlayerHandler()
        {
            // todo diversify this to include support for either
            var childComp = gameObject.GetComponentInChildren<LocalPlayerManager>();
            GameObject handler;
            if (!childComp)
            {
                handler = new GameObject("local player handler");
                handler.transform.parent = transform;
                childComp = handler.AddComponent<LocalPlayerManager>();
            }

            localPlayerManager = childComp;
            childComp.userType = localUserType;
            childComp.userId = clientUserId;
            childComp.CreatePlayerModel();
            
            localPlayerManager.onHandshake(this);
            // localPlayerManager.oscServer.StartServer();
        }

        public void SpawnAudienceHandler()
        {
            var childComp = GetComponentInChildren<AudienceManager>();
            if (!childComp)
            {
                var audienceHandler = new GameObject("External audience handler");
                audienceHandler.transform.parent = transform;
                childComp = audienceHandler.AddComponent<AudienceManager>();
            }

            audienceManager = childComp;

            // capture the value in a closure so it's addressed on handshake
            // (); += (sender, _) =>
            // {
            //     audienceManager.UpdateManager((ServerEventManager)sender);
            // };
            
            audienceManager.UpdateManager(serverEventManager);
            audienceManager.listenPort = localPorts.audience_motion_capture;
            audienceManager.clientUserId = clientUserId;
        }

        public void SpawnPerformerHandler()
        {
            var childComp = GetComponentInChildren<PerformerManager>();
            if (!childComp)
            {
                var performerHandler = new GameObject("External performer handler");
                performerHandler.transform.parent = transform;
                childComp = performerHandler.AddComponent<PerformerManager>();
            }
            performerManager = childComp;

           

            performerManager.UpdateManager(serverEventManager);
            performerManager.listenPort = localPorts.vrtp_data;
            performerManager.clientUserId = clientUserId;
        }
        
        

        public void Update()
        {
            if (!_consumedHandshake && _handshakeResult != null)
            {
                try
                {
                    HandleHandshakeCompletion(_handshakeResult);
                    
                }
                finally
                {
                    _consumedHandshake = true;
                }
            }
        }
    }

    [Serializable]
    public class HostSettings
    {
        public string remoteIP = "127.0.0.1";
        public ushort handshakePort = 5653;

        public IPEndPoint HandshakeEndPoint()
        {
            return new IPEndPoint(IPAddress.Parse(remoteIP), handshakePort);
        }
    }

    public enum UserType
    {
        Audience = 1,
        Performer,
    }
    
    
    
}