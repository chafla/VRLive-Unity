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
            
            #if SLIMEVR_ON_DESKTOP
            returner.slimeVRIP = "129.21.149.239";
            #elif SLIMEVR_ON_LAPTOP
            returner.slimeVRIP = "129.21.72.114";
            #else
            returner.slimeVRIP = manager.slimeVRHost;
            #endif
            
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

        public Vector3 WorldSpaceToOriginSpacePosition(Vector3 worldPosition)
        {
            return manager.xrOrigin.Origin.transform.InverseTransformPoint(worldPosition);
        }

        public Quaternion WorldSpaceToOriginSpaceRotation(Quaternion worldRotation)
        {
            return Quaternion.Inverse(manager.xrOrigin.Origin.transform.rotation) *
                   worldRotation;
        }

        public virtual void UpdateReturner()
        {
            // controllers (at least mine) seem to come in at what slimeVR interprets as an offset.
            // its "flat" value is somewhat off from what we expect to see.
            // for my controllers, it works out to be x=90, y=180, z=0.
            // yes this may cause gimbal lock because euler angles kinda suck but this is only here, so shhh
            var controllerRotationOffset = Quaternion.Euler(manager.ControllerRotationXOffset, manager.ControllerRotationYOffset, manager.ControllerRotationZOffset);
            var lControllerTf = manager.leftHandController.transform;
            var rControllerTf = manager.rightHandController.transform;
            var lConPos = lControllerTf.position;
            var lConRot = lControllerTf.rotation * controllerRotationOffset;
            var rConPos = rControllerTf.position;
            var rConRot = rControllerTf.rotation * controllerRotationOffset;
                
            var mocapData = new SlimeVRMocapReturner.MocapData(
                // head is fine though
                manager.xrOrigin.CameraInOriginSpacePos,
                WorldSpaceToOriginSpaceRotation(manager.xrOrigin.Camera.transform.rotation),
                WorldSpaceToOriginSpacePosition(lConPos),
                WorldSpaceToOriginSpaceRotation(lConRot),
                WorldSpaceToOriginSpacePosition(rConPos),
                WorldSpaceToOriginSpaceRotation(rConRot)
                    
            );
            returner.mocapDataIn.Enqueue(mocapData);
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
                UpdateReturner();
            }
            
            
        }
        
    }
}