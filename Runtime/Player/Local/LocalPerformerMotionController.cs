using System;
using EVMC4U;
using RTP;
using UnityEngine;
using uOSC;
using VRLive.Runtime.Utils;

namespace VRLive.Runtime.Player.Local
{
    public class LocalPerformerMotionController : MonoBehaviour
    {
        public ExternalReceiver vmcHandler;
        public VRMHMDTracker hmdManager;

        public LocalPlayerManager manager;

        public OscRelay oscRelay;
        public VRTPOscServer oscServer;

        public Parser oscParser = new Parser();

        private bool _hasHandshaked;
        
        public void Awake()
        {
            vmcHandler ??= gameObject.GetComponent<ExternalReceiver>() ?? gameObject.AddComponent<ExternalReceiver>();
            vmcHandler.Model = gameObject;

            oscServer = gameObject.GetComponent<VRTPOscServer>() ?? gameObject.AddComponent<VRTPOscServer>();
            

            var hmdManager = gameObject.GetComponent<VRMHMDTracker>() ?? gameObject.AddComponent<VRMHMDTracker>();

            // for (int i = 0; i < vmcHandler.NextReceivers.Length; i++)
            // {
            //     if (!vmcHandler.NextReceivers[i])
            //     {
            //         vmcHandler.NextReceivers[i] = this.hmdManager;
            //     }
            // }
            
            
        }

        public void OnNewRelayMessage(object _, VRTPData data)
        {
            oscServer.mocapDataIn.Enqueue(data);
        }

        public void OnHandshake()
        {
            oscRelay.OnNewMessage += OnNewRelayMessage;
            _hasHandshaked = true;
        }

        // public void OnEnable()
        // {
        //
        //     oscRelay.OnNewMessage += OnNewRelayMessage;
        // }

        public void Update()
        {
            if (!_hasHandshaked && manager && manager.hasHandshaked)
            {
                OnHandshake();
                _hasHandshaked = true;
            }
        }
    }
}