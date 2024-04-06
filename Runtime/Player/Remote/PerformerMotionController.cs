using System;
using EVMC4U;
using RTP;
using uOSC;

namespace VRLive.Runtime.Player
{
    public class PerformerMotionController : PlayerMotionController
    {
        public ExternalReceiver vmcHandler;

        public VRTPOscServer server;

        public override void Awake()
        {
            base.Awake();
            server ??= gameObject.AddComponent<VRTPOscServer>();
            vmcHandler ??= gameObject.GetComponent<ExternalReceiver>();
            vmcHandler ??= gameObject.AddComponent<ExternalReceiver>();
            

            checkRawData = true;

        }

        public void OnEnable()
        {
            vmcHandler.Model = gameObject;
            // server.StartServer();
        }

        // protected override void OnNewRawMocapData(Message msg)
        // {
        //     server.mocapDataIn.Enqueue(msg);
        //     // the OSC handler will take care of this
        //     return;
        //     // throw new System.NotImplementedException();
        // }


        protected override void OnNewMocapData(Message msg)
        {
            throw new NotImplementedException();
        }

        protected override void OnNewRawMocapData(VRTPData data)
        {
            server.mocapDataIn.Enqueue(data);
        }

        protected override void OnNewAudioData(VRTPData data)
        {
            // audio data is handled by the audio listener
            return;
            // throw new System.NotImplementedException();
        }

        // public override void Awake()
        // {
        //     base.Awake();
        //     // ExternalReceiver
        // }
    }
}