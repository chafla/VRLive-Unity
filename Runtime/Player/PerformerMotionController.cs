using EVMC4U;
using RTP;
using uOSC;

namespace VRLive.Runtime.Player
{
    public class PerformerMotionController : PlayerMotionController
    {
        protected override void OnNewMocapData(Message msg)
        {
            // the OSC handler will take care of this
            return;
            // throw new System.NotImplementedException();
        }
        

        protected override void OnNewAudioData(VRTPData data)
        {
            // audio data is handled by the audio listener
            return;
            // throw new System.NotImplementedException();
        }

        public override void Awake()
        {
            base.Awake();
            // ExternalReceiver
        }
    }
}