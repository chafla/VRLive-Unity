using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using RTP;
using UnityEngine;
using uOSC;
using Thread = System.Threading.Thread;

namespace VRLive.Runtime.Player.Local.SlimeVR
{
    /// <summary>
    /// A utility class that works in tandem with the relay to send data back to SlimeVR over a given port.
    ///
    /// SlimeVR is capable of getting information about where the HMD is by input and output ports.
    /// This means that we can, instead of trying to determine where that data is, deconstructing and reconstructing packets,
    /// we can instead just send the data back to slimeVR to have /it/ handle the reconstruction with the skeleton info it already has.
    ///
    /// This works under the assumption that you are running slimeVR on a computer local to you.
    /// If you aren't, that's kind of on you LOL
    /// </summary>
    public class SlimeVRMocapReturner : MonoBehaviour
    {

        public int slimeVRInputPort;

        public string slimeVRIP;

        public bool _sendActive = false;

        public bool sendWholeBundle = false;

        public MocapDataTypeExpected typeExpected;

        private Thread _thread;

        private ConcurrentQueue<VRTPData> oscDataOut;

        public ConcurrentQueue<MocapData> mocapDataIn;
        
        public void SendMocapDataThread()
        {
            // TODO find a way to integrate controllers into this as well
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.ReceiveTimeout = 5000;  // to check to see if we're running or not
            _sendActive = true;
            while (_sendActive)
            {
                var endpoint = new IPEndPoint(IPAddress.Parse(slimeVRIP), slimeVRInputPort);
                MocapData data;
                while (mocapDataIn.TryDequeue(out data))
                {
                    Bundle bundle;
                    switch (typeExpected)
                    {
                        case MocapDataTypeExpected.VRC:
                            bundle = data.GetBaseOSCMessage();
                            break;
                        case MocapDataTypeExpected.VRM:
                            bundle = data.GetVRMMessage();
                            break;
                        default:
                            Debug.LogError("Invalid mocap data type expected??");
                            continue;
                    }

                    var stream = new MemoryStream();
                    bundle.Write(stream);

                    if (!sendWholeBundle)
                    {
                        var parser = new Parser();
                        int pos = 0;
                        parser.Parse(stream.GetBuffer(), ref pos, (int)  stream.Position);
                        Message msg;
                        while ((msg = parser.Dequeue()).address != "")
                        {
                            // filthy filthy hack, just put messages into the queue why don't you
                            // doing this b/c I think our bundles aren't working
                            stream = new MemoryStream();
                            msg.Write(stream);
                            socket.SendTo(stream.GetBuffer()[..(int)stream.Position], endpoint);
                        }
                    }
                    else
                    {
                        socket.SendTo(stream.GetBuffer()[..(int)stream.Position], endpoint);
                    }
                    
                    
                    
                }
            }
        }

        public void OnDisable()
        {
            _sendActive = false;
        }

        public void Awake()
        {
            oscDataOut = new ConcurrentQueue<VRTPData>();
            mocapDataIn = new ConcurrentQueue<MocapData>();
        }

        public void StartThread()
        {
            _thread = new Thread(SendMocapDataThread);
            _thread.Start();
            Debug.Log("Starting up SlimeVRMocapReturner");
        }
        
        // public void StartThreads()
        // {
        //     Debug.Log($"Relay listening for incoming local mocap on {listeningPort}");
        //     Debug.Log($"Relaying mocap onto {destIP}:{destPort}");
        //     _sendThread = new Thread(SendMocapDataThread);
        //     _listenThread = new Thread(RecvMocapDataThread);
        //     if (!_sendActive)
        //         _sendThread.Start();
        //     if (!_listenActive)
        //         _listenThread.Start();
        // }

        public enum MocapDataTypeExpected
        {
            VRC,
            VRM
        }

        public class MocapData
        {
            // public Transform Head;

            public Vector3 HeadPos;

            public Quaternion HeadRot;

            // public Transform LController;

            public Vector3 LControllerPos;

            public Quaternion LControllerRot;

            // public Transform RController;

            public Vector3 RControllerPos;

            public Quaternion RControllerRot;

            public MocapData(Transform head, Transform lController, Transform rController)
            {
                HeadPos = head.position;
                HeadRot = head.rotation;
                LControllerPos = lController.position;
                LControllerRot = lController.rotation;

                RControllerPos = rController.position;
                RControllerRot = rController.rotation;
                // LController = lController;
                // RController = rController;
            }

            public bool PosHasRot = false;

            public static string HeadPosVRCDest = "/tracking/trackers/head/position";

            public static string HeadRotVRCDest = "/tracking/trackers/head/rotation";

            /// <summary>
            /// VRC thing that tracks the y position (and only the y position!) of the hmd
            /// </summary>
            public static string HMDPosition = "/avatar/parameters/Upright";

            public static string LControllerPosVRCDest = "/tracking/trackers/1/position";
            
            public static string LControllerRotVRCDest = "/tracking/trackers/1/rotation";

            public static string RControllerPosVRCDest = "/tracking/trackers/2/position";
            
            public static string RControllerRotVRCDest = "/tracking/trackers/2/rotation";

            public static string HeadVRMDest = "/VMC/Ext/Hmd/Pos";

            // values for these were gleaned from the values that slimeVR sent us so they better work
            // also, see https://github.com/SlimeVR/SlimeVR-Server/blob/f9c077e78bfa9488720cc862ca5cea3e6827476e/server/core/src/main/java/dev/slimevr/osc/VMCHandler.kt
            public static string VRMLeftHandSerial = "human://LEFT_HAND";

            public static string VRMRightHandSerial = "human://RIGHT_HAND";

            public static string VRMHMDSerial = "human://HEAD";

            public static string ControllerVRMDest = "/VMC/Ext/Con/Pos";

            private Message VRMMessage(string dest, string serial, Vector3 pos, Quaternion rot)
            {
                var msg = new Message(dest, new object[]
                {
                    serial, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w
                });

                return msg;
            }

            private void GetBaseOSCMessage(ref Bundle bundle, string posDest, string rotDest, Vector3 pos, Quaternion rot)
            {
                // var pos = tf.position;
                // var rot = tf.rotation;
                var posMsg = new Message(posDest, new object[]
                {
                    pos.x, pos.y, pos.z
                });

                var rotMsg = new Message(rotDest, new object[]
                {
                    rot.x, rot.y, rot.z, rot.w
                });
                
                bundle.Add(posMsg);
                bundle.Add(rotMsg);
            }

            public Bundle GetVRMMessage()
            {
                // vrm bundles position and rotation into one message
                var bundle = new Bundle(Timestamp.Now);


                // if (Head)
                // {
                    bundle.Add(VRMMessage(HeadVRMDest, VRMHMDSerial, HeadPos, HeadRot));
                // }

                // if (LController)
                // {
                    bundle.Add(VRMMessage(ControllerVRMDest, VRMLeftHandSerial, LControllerPos, LControllerRot));
                // }

                // if (RController)
                // {
                    bundle.Add(VRMMessage(ControllerVRMDest, VRMRightHandSerial, RControllerPos, RControllerRot));
                // }

                return bundle;
            }

            public Bundle GetBaseOSCMessage()
            {
                var bundle = new Bundle(Timestamp.Now);

                // if (Head)
                // {
                    // fill in the upright data
                    var uprightMsg = new Message(HMDPosition, new object[]
                    {
                        HeadPos.y,
                    });
                    
                    bundle.Add(uprightMsg);

                    GetBaseOSCMessage(ref bundle, HeadPosVRCDest, HeadRotVRCDest, HeadPos, HeadRot);
                // }
                //
                // if (LController)
                // {
                    GetBaseOSCMessage(ref bundle, LControllerPosVRCDest, LControllerRotVRCDest, LControllerPos, LControllerRot);
                // }

                // if (RController)
                // {
                    GetBaseOSCMessage(ref bundle, RControllerPosVRCDest, RControllerRotVRCDest, RControllerPos, RControllerRot);
                // }

                return bundle;
            }
        }
    }
}