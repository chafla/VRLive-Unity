using UnityEngine;

namespace VRLive.Runtime.Player.Local
{
    public class ScaleManager : MonoBehaviour
    {
        public HMDInputData inputData;

        public int scale;

        public bool lControllerButtonPressed;

        public bool rControllerButtonPressed;

        public float controllerDistance;

        public float initialControllerDistance;

        public Vector3 lControllerPos;

        public Vector3 rControllerPos;

        public bool scaling;

        public float scalingFactor = 0.3f;

        // public float scaledValue = 1;

        /// <summary>
        /// The scale factor that we have if we're not gripping; or, if we are gripping, the scaling factor that was in
        /// place before we started.
        /// </summary>
        public float bakedScale;

        public float ScaledValue => bakedScale + activeScaling;
        

        /// <summary>
        /// The amount of scaling that we're doing in our current grip
        /// </summary>
        public float activeScaling;

        public void Awake()
        {
            inputData = GetComponent<HMDInputData>() ?? gameObject.AddComponent<HMDInputData>();
            lControllerPos = Vector3.zero;
            rControllerPos = Vector3.zero;
        }
        
        public void Update()
        {
            inputData.leftController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton,
                out lControllerButtonPressed);
            inputData.rightController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton,
                out rControllerButtonPressed);
            
            inputData.leftController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition,
                out lControllerPos);
            
            inputData.rightController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition,
                out rControllerPos);

            controllerDistance = Vector3.Distance(lControllerPos, rControllerPos);

            if (!scaling && lControllerButtonPressed && rControllerButtonPressed)
            {
                scaling = true;
                initialControllerDistance = controllerDistance;
            }

            if (scaling && (!lControllerButtonPressed || !rControllerButtonPressed))
            {
                scaling = false;
                bakedScale += activeScaling;
                activeScaling = 0;
            }

            if (scaling)
            {
                activeScaling = scalingFactor * (controllerDistance - initialControllerDistance);
            }
            
        }
    }
}