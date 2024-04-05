using System;
using System.Collections.Generic;
using RTP;
using UnityEngine;
using uOSC;

namespace VRLive.Runtime.Player
{
    /// <summary>
    /// Controller managing instances of one type of client player.
    /// </summary>
    // [RequireComponent(typeof(RTPListener))]
    public abstract class RemotePlayerController : MonoBehaviour
    {
        public RTPListener listener;

        public ushort preInitListenPort;

        public ushort clientUserId;

        public string label;

        
        [Header("If true, users of this type will spawn locally as their own representations.")]
        public bool acceptLocalUser = true;

        public ushort listenPort
        {
            get => listener ? listener.listeningPort : preInitListenPort;
            set
            {
                if (listener)
                {
                    listener.listeningPort = value;
                    Debug.Log($"Remote player controller port updated to {value}");
                }
                else
                {
                    preInitListenPort = value;
                }
            }

        }

        public ServerEventManager manager;

        /// <summary>
        /// Map of user ID to players
        /// </summary>
        public Dictionary<int, PlayerMotionController> players = new Dictionary<int, PlayerMotionController>();

        public GameObject baseModel;


        
        public virtual void Awake()
        {
            if (!baseModel)
            {
                Debug.LogError($"{this} requires a base model!");
                return;
            }
            // members = new List<PlayerController>();
            
            players = new Dictionary<int, PlayerMotionController>();
        }

        public virtual void OnEnable()
        {
            
            listener = gameObject.AddComponent<RTPListener>();
            listener.label = label;
            listener.listeningPort = preInitListenPort;
            listener.OnNewData += OnNewListenerData;
            listener.StartServer();
            
            manager ??= GetComponentInParent<ServerEventManager>();
            manager ??= GetComponent<ServerEventManager>();
            if (manager)
                manager.OnNewServerEvent += OnNewServerEvent;
            else 
                Debug.LogWarning($"Server event manager was not found on {label} controller init, hope you're adding it later!");

        }

        public void UpdateManager(ServerEventManager newManager)
        {
            Debug.Log($"Adding server event manager for {label}");
            manager = newManager;
            manager.OnNewServerEvent += OnNewServerEvent;
        }

        public void OnNewServerEvent(object obj, Message msg)
        {
            int userId;
            UserType userType;
            switch (msg.address)
            {
                case "/server/status/useradd":
                    userId = (int) msg.values[0];
                    userType = (UserType)msg.values[1];
                    Debug.Log($"New user {userId}!");
                    if (userId == clientUserId)
                    {
                        Debug.Log("Got an add message for ourselves, ignoring!");
                        
                    }
                    CreateNewPlayer(userId, userType);
                    break;
                case "/server/status/userremove":
                    userId = (ushort)msg.values[0];
                    userType = (UserType)msg.values[1];
                    Debug.Log($"Goodbye {userId}!");
                    RemovePlayer(userId, userType);
                    break;
                    
            }
        }

        protected abstract void OnNewListenerData(object obj, VRTPPacket pkt);

        public void OnDisable()
        {
            if (manager)
                manager.OnNewServerEvent -= OnNewServerEvent;
        }

        public abstract void CreateNewPlayer(int userId, UserType usrType);

        public abstract void RemovePlayer(int userId, UserType usrType);
        // {
        //     switch (userType)
        //     {
        //         case UserType.Performer:
        //             throw new NotImplementedException();
        //         case UserType.Audience:
        //             var newObj = Instantiate(AudienceBase);
        //             var comp = newObj.GetComponent<AudiencePlayerController>() ?? newObj.AddComponent<AudiencePlayerController>();
        //             audienceMembers.Add(comp);
        //             break;
        //             // throw new NotImplementedException();
        //         default:
        //             throw new Exception($"Unknown usertype to instantiate: {userType}");
        //     }
        // }

        // public void OnListenerData(object src, VRTPPacket pkt);

        // public ConcurrentQueue<Message> mocapMessages;
    }
}