using System;
using EVMC4U;
using RTP;
using UnityEngine;
using uOSC;

namespace VRLive.Runtime.Player
{
    public class PerformerMotionController : PlayerMotionController
    {
        public ExternalReceiver vmcHandler;

        public VRTPOscServer server;

        public bool _waitingOnParent;

        private bool _usingTfMarker;

        // public PerformerManager manager;

        public override void Awake()
        {
            base.Awake();
            var obj = gameObject;
            server ??= obj.AddComponent<VRTPOscServer>();
            vmcHandler ??= obj.GetComponent<ExternalReceiver>();
            vmcHandler ??= obj.AddComponent<ExternalReceiver>();
            vmcHandler.Model = obj;

            var transformMarker = gameObject.GetComponentInChildren<PrefabTransformMarker>();
            if (transformMarker)
            {
                Debug.Log("Using transform marker, ignoring that found in parent.");
                var tf = transformMarker.gameObject.transform;
                vmcHandler.RootPositionTransform = tf;
                vmcHandler.RootRotationTransform = tf;
            }
            
            checkRawData = true;

        }

        public override void Update()
        {
            // ignore the expensiveness
            if (_waitingOnParent && parent != null)
            {
                vmcHandler.RootPositionTransform = parent.baseModelTransformRoot;
                vmcHandler.RootRotationTransform = parent.baseModelTransformRoot;
                vmcHandler.Model = gameObject;
                _waitingOnParent = false;
            }
            // print("a");
            base.Update();
        }

        public void OnEnable()
        {
            vmcHandler.Model = gameObject;
            // vmcHandler.RootPositionTransform = parent.baseModelTransformRoot;
            // vmcHandler.RootRotationTransform = parent.baseModelTransformRoot;
            // 
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