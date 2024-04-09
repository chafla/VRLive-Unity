using EVMC4U;
using UnityEngine;

namespace VRLive.Runtime.Utils
{
    public static class SlimeVRMessageProcessor
    {
        
        public static Vector3 DecodePosition(object[] values)
        {
            var x = (float) values[0];
            var y = (float) values[1];
            var z = (float) values[2];

            return new Vector3(x, y, z);
        }
        
        public static Quaternion DecodeRotation(object[] values)
        {
            float x, y, z, w;
            if (values.Length == 3)
            {
                x = (float) values[0];
                y = (float) values[1];
                z = (float) values[2];
                return Quaternion.Euler(x, y, z);
            }
            
            x = (float) values[0];
            y = (float) values[1];
            z = (float) values[2];
            w = (float) values[3];

            return new Quaternion(x, y, z, w);

        }
        
        public static void DisableDefaultCutBones(ExternalReceiver vmcHandler)
        {
            // these bones are cut by default on a new instance of the handler which is Really Annoying
            // make sure they're disabled for our purposes
            vmcHandler.CutBoneHips = false;
            vmcHandler.CutBoneSpine = false;
            vmcHandler.CutBoneChest = false;
            vmcHandler.CutBoneUpperChest = false;

            vmcHandler.CutBoneLeftUpperLeg = false;
            vmcHandler.CutBoneLeftLowerLeg = false;
            vmcHandler.CutBoneLeftFoot = false;
            vmcHandler.CutBoneLeftToes = false;

            vmcHandler.CutBoneRightUpperLeg = false;
            vmcHandler.CutBoneRightLowerLeg = false;
            vmcHandler.CutBoneRightFoot = false;
            vmcHandler.CutBoneRightToes = false;
        }

        public static void CutUnnecessaryBones(ExternalReceiver vmcHandler)
        {
            vmcHandler.CutBonesEnable = true;
            
            // argghhhhhh I don't know if this is all necessary but it's better to be safe than sorry

            // vmcHandler.CutBoneLeftHand = true;
            // vmcHandler.CutBoneRightHand = true;
            
            vmcHandler.CutBoneRightThumbProximal = true;
            vmcHandler.CutBoneRightThumbIntermediate = true;
            vmcHandler.CutBoneRightThumbDistal = true;

            vmcHandler.CutBoneRightIndexProximal = true;
            vmcHandler.CutBoneRightIndexIntermediate = true;
            vmcHandler.CutBoneRightIndexDistal = true;

            vmcHandler.CutBoneRightMiddleProximal = true;
            vmcHandler.CutBoneRightMiddleIntermediate = true;
            vmcHandler.CutBoneRightMiddleDistal = true;

            vmcHandler.CutBoneRightRingProximal = true;
            vmcHandler.CutBoneRightRingIntermediate = true;
            vmcHandler.CutBoneRightRingDistal = true;

            vmcHandler.CutBoneRightLittleProximal = true;
            vmcHandler.CutBoneRightLittleIntermediate = true;
            vmcHandler.CutBoneRightLittleDistal = true;
            
            vmcHandler.CutBoneLeftThumbProximal = true;
            vmcHandler.CutBoneLeftThumbIntermediate = true;
            vmcHandler.CutBoneLeftThumbDistal = true;

            vmcHandler.CutBoneLeftIndexProximal = true;
            vmcHandler.CutBoneLeftIndexIntermediate = true;
            vmcHandler.CutBoneLeftIndexDistal = true;

            vmcHandler.CutBoneLeftMiddleProximal = true;
            vmcHandler.CutBoneLeftMiddleIntermediate = true;
            vmcHandler.CutBoneLeftMiddleDistal = true;

            vmcHandler.CutBoneLeftRingProximal = true;
            vmcHandler.CutBoneLeftRingIntermediate = true;
            vmcHandler.CutBoneLeftRingDistal = true;

            vmcHandler.CutBoneLeftLittleProximal = true;
            vmcHandler.CutBoneLeftLittleIntermediate = true;
            vmcHandler.CutBoneLeftLittleDistal = true;
        }

    }
}