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

    }
}