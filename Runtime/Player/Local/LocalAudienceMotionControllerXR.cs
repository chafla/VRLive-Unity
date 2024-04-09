using System;
using System.Collections.Concurrent;
using RTP;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
// using UnityEngine.SpatialTracking;
using uOSC;
using VRLive.Runtime.Utils;

namespace VRLive.Runtime.Player.Local
{
    public class LocalAudienceMotionControllerXR : LocalAudienceMotionController
    {

        public InputActionAsset inputActions;
        public TrackedPoseDriver headTpd;

        public override void Awake()
        {
            base.Awake();
            SetUpHeadTracking();
            
            
            
        }

        // public override void Update()
        // {
        //     base.Update();
        //
        //     if (headTpd)
        //     {
        //         
        //     }
        //     
        // }

        public override void Update()
        {
            base.Update();
            var headOffset = head.transform.position - transform.position;
            transform.position = manager.xrOrigin.Camera.gameObject.transform.position - headOffset;// + new Vector3(0, eyeYOffset, 0);
            head.transform.rotation = manager.xrOrigin.Camera.gameObject.transform.rotation;
            
            oscRelay.Enqueue(PackageOSCData());
            // transform.rotation = manager.xrOrigin

            // gameObject.transform.position = head.position;
            // gameObject.transform.localPosition = head.localPosition;
            // headTpd.
        }

        public void SetUpHeadTracking()
        {
            var headObj = head.gameObject;

            // var tpd = headObj.AddComponent<TrackedPoseDriver>();
            // tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            // var actionMap = inputActions.FindActionMap("XRI HEAD");
            // if (actionMap == null)
            // {
            //     Debug.LogError("Action map must be of XRI Default Input Actions!");
            //     return;
            // }
            // tpd.positionAction = actionMap.FindAction("head - TPD - Position") ?? actionMap.FindAction("Position");
            // tpd.rotationAction = actionMap.FindAction("head - TPD - Rotation") ?? actionMap.FindAction("Rotation");
            // tpd.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
            // // manager.xrOrigin.Origin = gameObject;
            // headTpd = tpd;
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
        
        /// <summary>
        /// Take our current controller and HMD values and package them up as slimeVR data
        /// </summary>
        /// <returns></returns>
        public VRTPData PackageOSCData()
        {
            
            var bundle = new Bundle(Timestamp.Now);
            
            // assuming the head is at the origin, get the values based on that
            
            var originHeadPosition = head.transform.localPosition;

            var originHeadRotation = manager.xrOrigin.transform.rotation * WorldSpaceToOriginSpaceRotation(manager.xrOrigin.Camera.gameObject.transform.rotation);
                                     
            
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

            var localTf = transform;
            var rootPos = localTf.position;
            // as in Update(), account for the fact that our root is going to be slightly displaced based on our head
            var headOffset = head.transform.position - rootPos;
            var trueRootPos = manager.xrOrigin.Camera.gameObject.transform.position - headOffset;
            var rootPosMessage = new Message("/tracking/root/position",
                new object[] { trueRootPos.x, trueRootPos.y, trueRootPos.z });
            
            bundle.Add(rootPosMessage);
            
            var rootRot = localTf.rotation;
            var rootRotMessage = new Message("/tracking/root/rotation",
                new object[] { rootRot.x, rootRot.y, rootRot.z, rootRot.w });
            
            bundle.Add(rootRotMessage);
            
            // TODO fill in the rest of the data
    
            bundle.Add(headRotMessage);
            
            return VRTPData.FromBundle(bundle, manager.userId);
            
        }

    }
}