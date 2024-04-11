using System;
using UnityEngine;

namespace VRLive.Runtime.Player.Local.SlimeVR
{
    public class LocalPerformerIKManager : MonoBehaviour
    {
        protected Animator animator;

        // the bones on the model
        public GameObject lHandBone;
        public GameObject rHandBone;

        // where those bones should be 
        public GameObject lHandTarget;
        public GameObject rHandTarget;

        public void Start()
        {
            animator = GetComponent<Animator>();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!animator)
            {
                return;
            }

            if (lHandBone != null || lHandTarget != null)
            {
                if (lHandTarget == null || lHandBone == null)
                {
                    Debug.LogWarning("Can't do LH IK without both handbone and target");
                }
                else
                {
                    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                    var targetTf = lHandTarget.transform;
                    animator.SetIKPosition(AvatarIKGoal.LeftHand, targetTf.position);
                    animator.SetIKRotation(AvatarIKGoal.LeftHand, targetTf.rotation);
                    
                }
            }

            if (rHandBone != null || rHandTarget != null)
            {
                if (rHandTarget == null || rHandBone == null)
                {
                    Debug.LogWarning("Can't do RH IK without both handbone and target");
                }
                else
                {
                    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
                    var targetTf = rHandTarget.transform;
                    animator.SetIKPosition(AvatarIKGoal.RightHand, targetTf.position);
                    animator.SetIKRotation(AvatarIKGoal.RightHand, targetTf.rotation);
                }
            }
            
            
        }
    }
}