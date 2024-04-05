using RTP;
using UnityEngine;
using uOSC;

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
                            
                    head.localPosition = DecodePosition(msg.values);
                    break;
                case "/tracking/trackers/head/rotation":
                    head.localRotation = DecodeRotation(msg.values);
                    break;
                        
                case "/tracking/trackers/1/position":
                    lController.localPosition = DecodePosition(msg.values);
                    break;
                        
                case "/tracking/trackers/1/rotation":
                    lController.localRotation = DecodeRotation(msg.values);
                    break;
                        
                case "/tracking/trackers/2/position":
                    rController.localPosition = DecodePosition(msg.values);
                    break;
                        
                case "/tracking/trackers/2/rotation":
                    rController.localRotation = DecodeRotation(msg.values);
                    break;
            }
        }

        protected override void OnNewAudioData(VRTPData data)
        {
            Debug.LogWarning("Got audio data for an audience member, this should never happen");
        }
    }
}