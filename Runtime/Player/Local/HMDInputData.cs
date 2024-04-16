using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRLive.Runtime.Player.Local
{
    public class HMDInputData : MonoBehaviour
    {
        public InputDevice rightController;
        public InputDevice leftController;
        public InputDevice hmd;

        public bool usingOculus = false;

        private void Awake()
        {
            if (SystemInfo.deviceName.ToLower().Contains("oculus"))
            {
                usingOculus = true;
            }
        }

        void Update()
        {
            if (!rightController.isValid)
            {
                InitializeInputDevice(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right,
                    ref rightController);
            }

            if (!leftController.isValid)
            {
                InitializeInputDevice(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left,
                    ref leftController);
            }
            
            if (!hmd.isValid)
            {
                InitializeInputDevice(InputDeviceCharacteristics.HeadMounted, ref hmd);
            }
            
        }

        private void InitializeInputDevice(InputDeviceCharacteristics characteristics, ref InputDevice inputDevice)
        {
            List<InputDevice> devices = new List<InputDevice>();
            
            InputDevices.GetDevicesWithCharacteristics(characteristics, devices);

            if (devices.Count > 0)
            {
                inputDevice = devices[0];
            }
        }


    }
}