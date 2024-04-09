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
        // public VRMHMDTracker hmdManager;
        
        public VRTPOscServer oscServer;
        // private bool hasHandshaked;
        
        public override void Awake()
        {
            base.Awake();
            vmcHandler ??= gameObject.GetComponent<ExternalReceiver>() ?? gameObject.AddComponent<ExternalReceiver>();
            
            // kind of a bad place for this function to live but whatever
            SlimeVRMessageProcessor.DisableDefaultCutBones(vmcHandler);
            

            if (manager.cutHandBones)
            {
                vmcHandler.CutBonesEnable = true;
                SlimeVRMessageProcessor.CutUnnecessaryBones(vmcHandler);
                
            }
            
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