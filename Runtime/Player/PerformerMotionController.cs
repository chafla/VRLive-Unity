using EVMC4U;
using RTP;
using uOSC;

namespace VRLive.Runtime.Player
{
    public class PerformerMotionController : PlayerMotionController
    {
        protected override void OnNewMocapData(Message msg)
        {
            // I think the osc server should handle this by itself
            return;
            throw new System.NotImplementedException();
        }
        

        protected override void OnNewAudioData(VRTPData data)
        {
            // return;
            throw new System.NotImplementedException();
        }

        public override void Awake()
        {
            base.Awake();
            // ExternalReceiver
        }
    }
}