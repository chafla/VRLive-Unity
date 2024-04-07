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

    }
}