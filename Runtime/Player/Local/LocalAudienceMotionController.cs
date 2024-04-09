using System;
using System.Collections.Concurrent;
using RTP;
using UnityEngine;
using uOSC;
using VRLive.Runtime.Utils;

namespace VRLive.Runtime.Player.Local
{
    public class LocalAudienceMotionController : LocalPlayerMotionController
    {

        // the three transforms that will be coming out of our headset
        public Transform head;
        public Transform lController;
        public Transform rController;

        // use a concurrent message queue here since we can't set position from the callback's thread
        public ConcurrentQueue<Message> messageQueue;

        public override void Awake()
        {
            base.Awake();
            messageQueue = new ConcurrentQueue<Message>();
            // manager.xrOrigin.
        }
        
        

        public override void OnNewRelayMessage(object _, VRTPData data)
        {
            // todo
            int pos = 0;
            OscParser.Parse(data.Payload, ref pos, data.PayloadSize);
            Message msg;
            while ((msg = OscParser.Dequeue()).address != "")
            {
                messageQueue.Enqueue(msg);
            }
        }

        public override void Update()
        {
            base.Update();
            Message msg;
            while (messageQueue.TryDequeue(out msg))
            {
                OnNewMocapData(msg);
            }
        }
        
        // todo a lot of this code is stolen right from the remote audience motion controller 
        protected void OnNewMocapData(Message msg)
        {
            switch (msg.address)
            {
                case "/tracking/trackers/head/position":
                            
                    head.localPosition = SlimeVRMessageProcessor.DecodePosition(msg.values);
                    break;
                case "/tracking/trackers/head/rotation":
                    head.localRotation = SlimeVRMessageProcessor.DecodeRotation(msg.values);
                    break;
                        
                case "/tracking/trackers/1/position":
                    lController.localPosition = SlimeVRMessageProcessor.DecodePosition(msg.values);
                    break;
                        
                case "/tracking/trackers/1/rotation":
                    lController.localRotation = SlimeVRMessageProcessor.DecodeRotation(msg.values);
                    break;
                        
                case "/tracking/trackers/2/position":
                    rController.localPosition = SlimeVRMessageProcessor.DecodePosition(msg.values);
                    break;
                        
                case "/tracking/trackers/2/rotation":
                    rController.localRotation = SlimeVRMessageProcessor.DecodeRotation(msg.values);
                    break;
            }
        }
        
        
    }
}