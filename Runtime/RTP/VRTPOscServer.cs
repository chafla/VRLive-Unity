using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using uOSC;

namespace RTP
{
    // [RequireComponent(typeof(RTPListener))]
    public class VRTPOscServer : uOscServer
    {
        // public RTPListener Listener;
        public bool WaitForAudio = false;
        private Parser _parser;
        public ConcurrentQueue<VRTPData> mocapDataIn;

        public int maxMessagesPerBundle = 5;

        public int maxPacketsPerFrame = 1000;
        
        public event EventHandler<Message> OnNewMessageAvailable;

        private bool _active = false;
        private System.Threading.Thread _parseIncomingMocapThread;

        // [DoNotSerialize]
        // private new int port;
        
        void Awake()
        {
            _parser = new Parser();
        }
        
        void OnEnable()
        {
            
            var listener = GetComponent<RTPListener>();
            if (listener)
            {
                mocapDataIn = listener.MocapDataIn;
            }
            else
            {
                mocapDataIn = new ConcurrentQueue<VRTPData>();
            }

            _parseIncomingMocapThread = new System.Threading.Thread(ParseIncomingMocapThread);
            _parseIncomingMocapThread.Start();
            _active = true;
        }

        /// <summary>
        /// Spin off mocap parsing to a thread, since this is kind of an expensive task that 
        /// </summary>
        public void ParseIncomingMocapThread()
        {
            VRTPData data;
            while (_active)
            {
                while(mocapDataIn.TryDequeue(out data))
                {
                    var pos = 0;
                    // TODO may be worth revisiting this at some point to remove this weird indirection
                    _parser.Parse(data.Payload, ref pos, data.PayloadSize);
                }
            }
           
        }

        void OnDisable()
        {
            _active = false;
        }
        void Update()
        {
            VRTPData data;
            Message emptyMessage;

            // this is kind of a hack, but it should delay mocap by the DSP delay, hopefully?
            // it seems to be sitting at about 40ms in testing.
            
            // The big problem here is mostly that this skips frames...
            // if (WaitForAudio && Listener.MocapDataIn.TryPeek(out data))
            // {
            //     var dt = DateTime.Now - data.Arrived;
            //     // buflength is the buffer size in samples
            //     int bufLength;
            //     AudioSettings.GetDSPBufferSize(out bufLength, out _);   
            //     // divide it by the sample rate (samples / second) to get the amount of time in seconds
            //     float delayMs = (float) bufLength / 48000;
            //     if (dt.TotalMilliseconds / 1000 < delayMs)
            //     {
            //         Debug.Log($"Pausing on mocap packet, it's not ready yet. {dt.TotalMilliseconds / 1000}, {AudioSettings.dspTime}");
            //         return;
            //     }
            // }
            var packetsRead = 0;
            // Debug.Log($"Incoming mocap pressure: {Listener.MocapDataIn.Count}");
            Message msg;
            while (_active && packetsRead++ < maxPacketsPerFrame && (msg = _parser.Dequeue()).address != "")
            {
                // var message = _parser.Dequeue();
                // ensure they're marked with the time it came in
                // if (message.timestamp.value == 0)
                // {
                //     message.timestamp = Timestamp.Now;
                // }
                // Debug.Log($"OSC Message in: {message}");
                onDataReceived.Invoke(msg);
                // OnNewMessageAvailable?.Invoke(this, message);
                    
#if UNITY_EDITOR
                _onDataReceivedEditor.Invoke(msg);
#endif
            }
            
            if (packetsRead >= maxPacketsPerFrame)
            {
                Debug.LogWarning($"Current mocap pressure after parsing: {mocapDataIn.Count}");
            }
            // while (_active && !mocapDataIn.IsEmpty && packetsRead++ < maxPacketsPerFrame)
            // {
                // if (WaitForAudio && mocapDataIn.TryPeek(out data))
                // {
                //     var dt = DateTime.Now - data.Arrived;
                //     // buflength is the buffer size in samples
                //     int bufLength;
                //     AudioSettings.GetDSPBufferSize(out bufLength, out _);   
                //     // divide it by the sample rate (samples / second) to get the amount of time in seconds
                //     float delayMs = (float) bufLength / 44000;
                //     if (dt.TotalMilliseconds / 1000 < delayMs)
                //     {
                //         // Debug.Log($"Pausing on mocap packet, it's not ready yet. {dt.TotalMilliseconds / 1000} {delayMs}");
                //         // Listener.MocapDataIn.Enqueue(data);  // todo this may put it out of order...
                //         break;
                //     }
                // }

               

                // List<Message> messagesThisFrame = new List<Message>();
                
                // loop over everything to clear the queue
//                 while ((msg = _parser.Dequeue()).address != "")
//                 {
//                     // var message = _parser.Dequeue();
//                     // ensure they're marked with the time it came in
//                     // if (message.timestamp.value == 0)
//                     // {
//                     //     message.timestamp = Timestamp.Now;
//                     // }
//                     // Debug.Log($"OSC Message in: {message}");
//                     onDataReceived.Invoke(msg);
//                     // OnNewMessageAvailable?.Invoke(this, message);
//                     
// #if UNITY_EDITOR
//                 _onDataReceivedEditor.Invoke(msg);
// #endif
//                 }

            
        }

        // stub out the parent implementation: we don't want it to start anything
        new void StartServer()
        {
            
        }

        new void StopServer()
        {
            
        }
    }
}