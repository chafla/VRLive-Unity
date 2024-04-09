using System;
using EVMC4U;
using RTP;
using UnityEngine;
using uOSC;
using VRLive.Runtime.Utils;

namespace VRLive.Runtime.Player.Local
{
    public class LocalPerformerMotionController : LocalPlayerMotionController
    {
        public ExternalReceiver vmcHandler;
        public VRMHMDTracker hmdManager;
        
        public VRTPOscServer oscServer;
        // private bool hasHandshaked;
        
        public virtual void Awake()
        {
            vmcHandler ??= gameObject.GetComponent<ExternalReceiver>() ?? gameObject.AddComponent<ExternalReceiver>();
            vmcHandler.Model = gameObject;

            oscServer = gameObject.GetComponent<VRTPOscServer>() ?? gameObject.AddComponent<VRTPOscServer>();
            // var hmdManager = gameObject.GetComponent<VRMHMDTracker>() ?? gameObject.AddComponent<VRMHMDTracker>();
        }

        public override void OnNewRelayMessage(object _, VRTPData data)
        {
            oscServer.mocapDataIn.Enqueue(data);
        }
    }
}