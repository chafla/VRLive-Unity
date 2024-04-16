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
        
        // this stuff has to do with trying to fit as many messages as we can into a bundle without going over MTU.
        public Bundle workingBundle;

        private int curMessagesInBundle = 0;
        public int maxMessagesPerBundle = 5;
        
        /// <summary>
        /// If this is true, then any data coming in and processed by the VRTPOscServer will be directed, message by message,
        /// to be output by the OSC relay.
        ///
        /// The main motivation for crossing wires like this is to prevent having to decode messages /again/ in the relay,
        /// when we've already done so in order to pass it along to an avatar. This is also in the service of sending smaller
        /// VRM packets that fit under MTU, since we've already run into problems with IP fragmentation and it seems to basically
        /// make performer mocap transmission unusable over the general network.
        ///
        /// This should be primarily used for performers.
        /// </summary>
        public bool sendFromOscServerToRelay = false;

        private bool _sendFromOscServerPrevious = false;
        
        public virtual void Awake()
        {
            workingBundle = new Bundle(Timestamp.Now);
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

            // if (sendFromOscServerToRelay)
            // {
                // prevent it from re-ingesting its own data, since we want to give it our own data to work with
                // manager.relay.immediatelyRequeueRawData = false;
                // oscServer.OnNewMessageAvailable += PassMessageToRelay;
            // }

            // _sendFromOscServerPrevious = sendFromOscServerToRelay;

        }

        public override void Update()
        {
            base.Update();
            // if (_sendFromOscServerPrevious != sendFromOscServerToRelay)
            // {
            //     _sendFromOscServerPrevious = sendFromOscServerToRelay;
            //     if (!sendFromOscServerToRelay)
            //     {
            //         
            //         oscServer.onDataReceived.RemoveListener(PassMessageToRelay);
            //         manager.relay.immediatelyRequeueRawData = true;
            //        
            //     }
            //     else
            //     {
            //         // prevent it from re-ingesting its own data, since we want to give it our own data to work with
            //         manager.relay.immediatelyRequeueRawData = false;
            //         oscServer.onDataReceived.AddListener(PassMessageToRelay);
            //     }
            //     
            //
            // }

        }

        public void PassMessageToRelay(Message msg)
        {
            // convert it to a bundle, since that's timestamped and messages are not
            // plus it's what most of our system expects to see
            // this also helps to reduce pressure on the single thread consuming mocap data,
            // which tbh should probably be offloaded to a threadpool or something TODO
            workingBundle.Add(msg);
            curMessagesInBundle++;
            if (curMessagesInBundle >= maxMessagesPerBundle)
            {
                // full send
                oscRelay.Enqueue(VRTPData.FromBundle(workingBundle, manager.userId));
                workingBundle = new Bundle(Timestamp.Now);
                curMessagesInBundle = 0;
            }
            // var bundle = new Bundle(Timestamp.Now);
            // bundle.Add(msg);
            // oscRelay.Enqueue(VRTPData.FromBundle(bundle, manager.userId));
        }
        
        public override void OnNewRelayMessage(object _, VRTPData data)
        {
            // if (!sendFromOscServerToRelay)
            oscServer.mocapDataIn.Enqueue(data);
        }
    }
}