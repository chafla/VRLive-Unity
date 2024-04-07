﻿using RTP;
using UnityEngine;
using uOSC;
using VRLive.Runtime.Utils;

namespace VRLive.Runtime.Player
{
    public class AudienceMotionController : PlayerMotionController
    {
        public Transform head;
        public Transform lController;
        public Transform rController;

       

        // private Transform HeadTempTransform;
        // private Transform LContTempTransform;
        // private Transform RContTempTransform;
        
        protected override void OnNewMocapData(Message msg)
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

        protected override void OnNewRawMocapData(VRTPData data)
        {
            return;
        }

        protected override void OnNewAudioData(VRTPData data)
        {
            Debug.LogWarning("Got audio data for an audience member, this should never happen");
        }
    }
}