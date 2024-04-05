using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using uOSC;

namespace VRLive.Runtime
{
    [RequireComponent(typeof(ServerEventListener))]
    public class ServerEventManager : MonoBehaviour
    {
        // todo move this out to an abstract class
        public ServerEventListener Listener;
        public List<Message> ServerMessages { get; private set; }
        
        public event EventHandler<Message> OnNewServerEvent; 

        public void Awake()
        {
            ServerMessages = new List<Message>();
            Listener = gameObject.GetComponent<ServerEventListener>();
            Listener.NewDataAvailable += OnNewData;
        }

        private void OnNewData(object ls, ConcurrentQueue<Message> queue)
        {
            Message msg;
            var nextMsg = queue.TryDequeue(out msg);
            if (!nextMsg)
            {
                Debug.LogError($"Failed to dequeue the next server event!");
                return;
            }
            ServerMessages.Add(msg);
            OnNewServerEvent?.Invoke(this, msg);
            Debug.LogWarning($"New server message {msg}");
            
            
        }
    }
}