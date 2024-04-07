using System;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using VRLive.Runtime.Player.Local;
using VRM;

namespace VRLive.Runtime.Utils
{
    public class VRMHMDTracker : MonoBehaviour, IVRMComponent
    {
        // takes a lot from vrm/lookathead
        public HMDInputData inputData;

        [SerializeField] public Transform head;

        public Transform leftHand;

        public Transform rightHand;

        public XROrigin origin;

        public InputActionManager ActionManager;

        public void GetBone(Animator animator, ref Transform tf, HumanBodyBones bone)
        {
            var boneTf = animator.GetBoneTransform(bone);

            if (boneTf == null)
            {
                Debug.LogWarning($"Missing a required bone, was looking for {bone}!");
                return;
            }

            tf = boneTf;

        }

        public void Awake()
        {
            var animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("animator is not found");
                return;
            }

            if (origin == null)
            {
                Debug.LogWarning("Need an xr origin!");
                return;
            }
            
            GetBone(animator, ref head, HumanBodyBones.Head);
            GetBone(animator, ref leftHand, HumanBodyBones.LeftHand);
            GetBone(animator, ref rightHand, HumanBodyBones.RightHand);
            
            
            // var aniHead = animator.GetBoneTransform(HumanBodyBones.Head);
            //
            // if (aniHead == null)
            // {
            //     Debug.LogWarning("head is not found");
            //     return;
            // }
            //
            // head = aniHead;
            //
            // var 
            
            
            // throw new NotImplementedException();
        }

        public void OnImported(VRMImporterContext context)
        {
            var gltfFirstPerson = context.VRM.firstPerson;
        }

        public void OnEnable()
        {
            inputData ??= gameObject.AddComponent<HMDInputData>();
        }

        public void Update()
        {
            // inputData.hmd.
        }
    }
}