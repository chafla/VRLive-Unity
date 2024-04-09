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

    }
}