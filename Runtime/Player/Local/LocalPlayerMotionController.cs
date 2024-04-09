using RTP;
using Unity.VisualScripting;
using UnityEngine;
using uOSC;
using VRLive.Runtime.Player.Local.SlimeVR;

namespace VRLive.Runtime.Player.Local
{
    public abstract class LocalPlayerMotionController : MonoBehaviour
    {
        public LocalPlayerManager manager;

        public float eyeYOffset;
        
        /// <summary>
        /// Our OSC relay. Should be set on handshake.
        /// </summary>
        public OscRelay oscRelay;

        public SlimeVRMocapReturner returner;
        
        protected Parser OscParser = new Parser();

        protected bool hasHandshaked;

        public abstract void OnNewRelayMessage(object _, VRTPData data);

        public virtual void Awake()
        {
            if (!returner)
            {
                StartReturner();
            }
        }

        public void StartReturner()
        {
            returner = GetComponent<SlimeVRMocapReturner>() ?? gameObject.AddComponent<SlimeVRMocapReturner>();
            returner.slimeVRIP = manager.slimeVRHost;
            switch (manager.userType)
            {
                case UserType.Audience:
                    returner.typeExpected = SlimeVRMocapReturner.MocapDataTypeExpected.VRC;
                    returner.slimeVRInputPort = manager.slimeVrVrcMocapInPort;
                    break;
                case UserType.Performer:
                    returner.typeExpected = SlimeVRMocapReturner.MocapDataTypeExpected.VRM;
                    returner.slimeVRInputPort = manager.slimeVrVrmMocapInPort;
                    break;
                default:
                    Debug.LogError("Unexpected item in bagging area");
                    break;
            }
        }
        
        public virtual void OnHandshake()
        {
            oscRelay.OnNewMessage += OnNewRelayMessage;
            hasHandshaked = true;
            if (!returner)
            {
                StartReturner();
            }
            returner.StartThread();
        }

        
        public virtual void Update()
        {
            if (!hasHandshaked && manager && manager.hasHandshaked)
            {
                OnHandshake();
                hasHandshaked = true;
            }

            if (returner)
            {
                returner.mocapDataIn.Enqueue(new SlimeVRMocapReturner.MocapData(manager.xrOrigin.Camera.gameObject.transform, manager.leftHandController.transform, manager.rightHandController.transform));
            }
            
            
        }
        
    }
}