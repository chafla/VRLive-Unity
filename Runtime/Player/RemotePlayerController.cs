﻿using System.Collections.Generic;
using RTP;
using UnityEngine;
using uOSC;

namespace VRLive.Runtime.Player
{
    /// <summary>
    /// Controller managing instances of one kind of client player.
    /// </summary>
    // [RequireComponent(typeof(RTPListener))]
    public abstract class RemotePlayerController : MonoBehaviour
    {
        public RTPListener listener;

        public ushort preInitListenPort;

        public ushort clientUserId;

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
            
            // listener = gameObject.AddComponent<RTPListener>();
            // listener.listeningPort = preInitListenPort;
            // listener.OnNewData += OnNewListenerData;
            // listener.StartServer();
            
            // manager ??= GetComponentInParent<ServerEventManager>();
            // manager ??= GetComponent<ServerEventManager>();
            // manager.OnNewServerEvent += OnNewServerEvent;

            players = new Dictionary<int, PlayerMotionController>();

            
        }

        public void UpdateManager(ServerEventManager newManager)
        {
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