using System;
using System.Net;
using JetBrains.Annotations;
using RTP;
using UnityEngine;
using VRLive.Runtime.Player;

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

        public UserType userType;

        public ushort clientUserId;

        public ClientPortMap localPorts;

        public HostSettings hostSettings;
        
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

        public RemotePlayerController remotePlayerController;
        
        #endregion

        private bool _consumedHandshake = false;

        [CanBeNull] private HandshakeResult _handshakeResult;

        public GameObject baseAudienceObjectType;

        public GameObject basePerformerObjectType;

        public void Awake()
        {
            HandshakeManager = new HandshakeManager(hostSettings.HandshakeEndPoint(), localPorts, userType, clientIdentifier);
            HandshakeManager.OnHandshakeCompletion += OnHandshakeSuccessEvent;
            HandshakeManager.RunHandshake();
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
            
            backingTrackManager ??= gameObject.AddComponent<BackingTrackManager>();
            // TODO clean this up in a way that doesn't involve as much duplication, maybe by passing some value in
            backingTrackManager.Listener.Port = result.serverPorts.backing_track_conn_port;
            backingTrackManager.Listener.Host = hostSettings.remoteIP;
            backingTrackManager.Listener.StartListener();
            serverEventManager ??= gameObject.AddComponent<ServerEventManager>();
            serverEventManager.Listener.Port = result.serverPorts.server_event_conn_port;
            serverEventManager.Listener.Host = hostSettings.remoteIP;
            serverEventManager.Listener.StartListener();

            rtpListener ??= gameObject.AddComponent<RTPListener>();
            rtpListener.listeningPort = localPorts.vrtp_data;
            rtpListener.StartServer();

            // these both rely on the rtp listener
            oscServer ??= gameObject.AddComponent<VRTPOscServer>();
            rtpAudioListener ??= gameObject.AddComponent<RTPAudioListenerComponentized>();
            
            
            if (userType == UserType.Performer)
            {
                // this one's a bit more dynamic
                audioStreamer ??= gameObject.AddComponent<AudioStreamer>();
                audioStreamer.sendServerPort = result.serverPorts.performer_audio_in;
                audioStreamer.sendServerIP = hostSettings.remoteIP;
                // audioStreamer.s
            }
            
            SpawnAudienceHandler();
            
            SpawnPerformerHandler();

            // create the remaining players.
            foreach (var user in result.OtherUsers)
            {
                switch (user.user_type)
                {
                    case UserType.Audience:
                        audienceManager.CreateNewPlayer(user.user_id, user.user_type);
                        break;
                    case UserType.Performer:
                        performerManager.CreateNewPlayer(user.user_id, user.user_type);
                        break;
                    default:
                        Debug.LogWarning("Got an invalid user type when trying to add other users!");
                        break;
                }
               
            }
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