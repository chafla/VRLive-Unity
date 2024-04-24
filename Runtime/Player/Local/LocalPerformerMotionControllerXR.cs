using System;
using RTP;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.UIElements;
using uOSC;
using VRLive.Runtime.Player.Local.SlimeVR;

namespace VRLive.Runtime.Player.Local
{
    public class LocalPerformerMotionControllerXR : LocalPerformerMotionController
    {

        public bool useLocalHead = true;

        public bool convToOriginSpace = false;

        public Transform rootTransform;

        public Transform rootRotation;

        public bool rootRotationOnlyAlongY;

        public bool useLocalTransform;

        public bool onlyOrigin;

        public float localTransformMultiplicativeFactor;

        private bool _scalingRayActive = false;

        public ScaleManager scaleManager;

        public LineRenderer scalingRay;

        public override void Awake()
        {
            base.Awake();

            // we /need/ this for our VRM setup
            sendFromOscServerToRelay = true;

            scaleManager = GetComponent<ScaleManager>() ?? gameObject.AddComponent<ScaleManager>();

            // turns out this is the best way to track the root transform, who'd have thunk
            rootTransform = manager.xrOrigin.transform;
            
            // var headTf = animator.GetBoneTransform(HumanBodyBones.Head);
            // gameObject.transform.position = manager.xrOrigin.Camera.gameObject.transform.position;// + new Vector3(0, eyeYOffset, 0);
            // head = animator.SetBoneLocalRotation()
            // todo find a good way to selectively allow slimevr to send head data if it is not zeroed out

            // if (useLocalHead)
            // {
            // slimevr feeds us useless data here:
            /*
             * oscArgs.add("root")
					addTransformToArgs(
						NULL,
						IDENTITY,
					)
					oscBundle
						.addPacket(
							OSCMessage(
								"/VMC/Ext/Root/Pos",
								oscArgs.clone(),
							),
						)

             */
            // so we have to do it ourselves
            vmcHandler.RootPositionSynchronize = false;
            vmcHandler.RootRotationSynchronize = false;
            
            // this is a custom edit to hack around the fact that scale always gets set to zero when we're working with slimevr
            vmcHandler.IgnoreRootScaleOffset = true;

            scalingRay = gameObject.AddComponent<LineRenderer>();
            scalingRay.alignment = LineAlignment.TransformZ;

            // vmcHandler.Scale
            // }
        }

        public override void UpdateReturner()
        {
	        var controllerRotationOffset = Quaternion.Euler(manager.ControllerRotationXOffset, manager.ControllerRotationYOffset, manager.ControllerRotationZOffset);
	        var lControllerTf = manager.leftHandController.transform;
	        var rControllerTf = manager.rightHandController.transform;
	        var lConPos = lControllerTf.position;
	        var lConRot = lControllerTf.rotation * controllerRotationOffset;
	        var rConPos = rControllerTf.position;
	        var rConRot = rControllerTf.rotation * controllerRotationOffset;
	        
	        var mocapData = new VMCMocapData(
		        // head is fine though
		        manager.xrOrigin.CameraInOriginSpacePos,
		        WorldSpaceToOriginSpaceRotation(manager.xrOrigin.Camera.transform.rotation),
		        WorldSpaceToOriginSpacePosition(lConPos),
		        WorldSpaceToOriginSpaceRotation(lConRot),
		        WorldSpaceToOriginSpacePosition(rConPos),
		        WorldSpaceToOriginSpaceRotation(rConRot),
		        gameObject.transform.localPosition,
		        gameObject.transform.localRotation,
		        gameObject.transform.localScale
	        );
	        
	        returner.mocapDataIn.Enqueue(mocapData);

	        var bundle = new Bundle();
	        mocapData.AddRootMessage(ref bundle);
	        oscRelay.Enqueue(VRTPData.FromBundle(bundle, manager.userId));
        }

        public override void Update()
        {
            base.Update();
            // if (useLocalHead)
            // {
            if (rootTransform)
            {
	            // var tf = WorldSpaceToOriginSpacePosition()
	            var tf = (useLocalTransform ? rootTransform.transform.localPosition : rootTransform.transform.position);
	            if (convToOriginSpace)
	            {
		            tf = WorldSpaceToOriginSpacePosition(tf);
	            }

	            if (vmcHandler.RootPositionTransform)
	            {
		            // this isn't always set the first time we hit it so better safe than sorry
		            vmcHandler.RootPositionTransform.localPosition = tf;
	            }
	            



            }

            if (onlyOrigin)
            {
	            vmcHandler.RootPositionTransform.localPosition = -manager.xrOrigin.Origin.transform.position;
            }

            if (rootRotation)
            {
	            // this works best when set to the xr origin's rotation by default, since it only rotates on snap turns.
	            vmcHandler.RootRotationTransform.localRotation = rootRotation.transform.rotation;
            }

            if (localTransformMultiplicativeFactor > 0)
            {
	            vmcHandler.RootPositionTransform.localPosition *= localTransformMultiplicativeFactor;
            }

            if (scaleManager)
            {
	            // if (scaleManager.scaling)
	            // {
		           //  if (!scalingRay.enabled)
		           //  {
			          //   scalingRay.enabled = true;
		           //  }
		           //  scalingRay.SetPosition(0, manager.xrOrigin.Camera.transform.position);
		           //  scalingRay.SetPosition(1, manager.xrOrigin.Camera.transform.position + Vector3.forward * 5);
	            // }
	            // else
	            // {
		           //  if (scalingRay.enabled)
		           //  {
			          //   scalingRay.enabled = false;
		           //  }
	            // }
	            
	            gameObject.transform.localScale = Vector3.one * Math.Min(Math.Max(scaleManager.ScaledValue, 0.7f), 5.0f);
            }
            
            // gameObject.transform.position = manager.xrOrigin.Camera.transform.position;// + new Vector3(0, eyeYOffset, 0);
            // }
        }

        // public void InterceptBuffer()
        // {
	       //  var bundle = new Bundle();
	       //  bundle.Add(new Message(
		      //   RootPosVRM,
		      //   new object[]
		      //   {
			     //    "root", OriginPos.x, OriginPos.y, OriginPos.z, OriginRot.x, OriginRot.y, OriginRot.z, OriginRot.w, Scale.x, Scale.y, Scale.z, 0, 0, 0
		      //   }));
        // }

        // public void OnAnimatorIK(int layerIndex)
        // {
        //     return;
        //     // throw new NotImplementedException();
        // }
        
        
            public class VMCMocapData : SlimeVRMocapReturner.MocapData
            {
                public Vector3 Scale = Vector3.one;
                public Quaternion OriginRot = Quaternion.identity;
                public Vector3 OriginPos = Vector3.zero;
                
                
                public static string RootPosVRM = "/VMC/Ext/Root/Pos";

                public VMCMocapData(Vector3 headPos, Quaternion headRot, Vector3 lControllerPos, Quaternion lControllerRot, Vector3 rControllerPos, Quaternion rControllerRot, Vector3 originPos, Quaternion originRot) : base(headPos, headRot, lControllerPos, lControllerRot, rControllerPos, rControllerRot)
                {
                    OriginPos = originPos;
                    OriginRot = originRot;
                }
                
                public VMCMocapData(Vector3 headPos, Quaternion headRot, Vector3 lControllerPos, Quaternion lControllerRot, Vector3 rControllerPos, Quaternion rControllerRot, Vector3 originPos, Quaternion originRot, Vector3 scale) : this(headPos, headRot, lControllerPos, lControllerRot, rControllerPos, rControllerRot, originPos, originRot)
                {
                    OriginPos = originPos;
                    OriginRot = originRot;
                    Scale = scale;
                }

                public void AddRootMessage(ref Bundle bundle)
                {
	                bundle.Add(new Message(
		                RootPosVRM,
		                new object[]
		                {
			                // NOTE!
			                // rootVRL (instead of just root) is deviation from standard vmc protocol
			                // but this is necessary to get around slimeVR's mistreatment of vmc data
			                "rootVRL", OriginPos.x, OriginPos.y, OriginPos.z, OriginRot.x, OriginRot.y, OriginRot.z, OriginRot.w, 1.0f / Scale.x,  1.0f / Scale.y, 1.0f / Scale.z, 0f, 0f, 0f
		                }));
                    

	                // return bundle;
                }

                public override Bundle GetVRMMessage()
                {
                    var bundle =  base.GetVRMMessage();
                    
                    // add in the position and scale.
                    // this is all information that SlimeVR kinda inconveniently deprives us of, but if we've calculated it locally
                    // we can calculate it oursevles and pass it along to the server for the remote externalreceivers to pick up on
                    // note that the last three zeros are for scale offset, which is something we don't really worry about
                    AddRootMessage(ref bundle);
                    return bundle;
                }
                
            }

    }
}