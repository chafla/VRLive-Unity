using System;
using RTP;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using uOSC;

namespace VRLive.Runtime.Player.Local
{
    public class LocalPerformerMotionControllerXR : LocalPerformerMotionController
    {
        public InputActionAsset inputActions;
        public TrackedPoseDriver headTpd;
        protected Animator animator;

        protected GameObject head;
        protected GameObject lArm;
        protected GameObject rArm;

        public bool useLocalHead = true;

        public override void Awake()
        {
            base.Awake();
            animator = gameObject.GetComponent<Animator>();
            
            var headTf = animator.GetBoneTransform(HumanBodyBones.Head);
            gameObject.transform.position = manager.xrOrigin.Camera.gameObject.transform.position;// + new Vector3(0, eyeYOffset, 0);
            // head = animator.SetBoneLocalRotation()
            // todo find a good way to selectively allow slimevr to send head data if it is not zeroed out

            if (useLocalHead)
            {
                vmcHandler.RootPositionSynchronize = false;
                vmcHandler.RootRotationSynchronize = false;
            }
        }
        
        public override void Update()
        {
            base.Update();
            if (useLocalHead)
            {
                gameObject.transform.position = manager.xrOrigin.Camera.gameObject.transform.position;// + new Vector3(0, eyeYOffset, 0);
                
                
            }
                
        }

        public void OnAnimatorIK(int layerIndex)
        {
            return;
            // throw new NotImplementedException();
        }

        /// <summary>
        /// Take our current controller and HMD values and package them up as slimeVR data
        /// </summary>
        /// <returns></returns>
        public VRTPData PackageOSCData()
        {
            
            var bundle = new Bundle(Timestamp.Now);
            
            // assuming the head is at the origin, get the values based on that
            
            var originHeadPosition = manager.xrOrigin.CameraInOriginSpacePos;

            var originHeadRotation = Quaternion.Inverse(manager.xrOrigin.Origin.transform.rotation) *
                                     manager.xrOrigin.Camera.transform.rotation;
            
            // following the same transformation done to get the camera in terms of origin space

            var originLHandPosition =
                manager.xrOrigin.Origin.transform.InverseTransformPoint(manager.leftHandController.transform.position);
            
            var originRHandPosition =
                manager.xrOrigin.Origin.transform.InverseTransformPoint(manager.rightHandController.transform.position);
            
            

            var headPosMessage = new Message("/tracking/trackers/head/position",
                new object[] { originHeadPosition.x, originHeadPosition.y, originHeadPosition.z });
            
            bundle.Add(headPosMessage);
            
            var headRotMessage = new Message("/tracking/trackers/head/rotation",
                new object[] { originHeadRotation.x, originHeadRotation.y, originHeadRotation.z, originHeadRotation.w });
            
            // TODO fill in the rest of the data
    
            bundle.Add(headRotMessage);
            
            return VRTPData.FromBundle(bundle, manager.userId);
            
        }
    }
}