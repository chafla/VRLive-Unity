using RTP;
using UnityEngine;
using uOSC;

namespace VRLive.Runtime.Player
{
    public class LocalPlayerController : PlayerMotionController
    {
        public GameObject localObj;
        
        


        protected override void OnNewMocapData(Message msg)
        {
            return;
        }

        protected override void OnNewRawMocapData(VRTPData data)
        {
            
        }

        protected override void OnNewAudioData(VRTPData data)
        {
            
        }
    }
}