using System;
using Unity.VisualScripting;
using UnityEngine;
using uOSC;

namespace RTP
{
    [RequireComponent(typeof(RTPListener))]
    public class VRTPOscServer : uOscServer
    {
        public RTPListener Listener;
        public bool WaitForAudio = false;
        private Parser _parser;

        // [DoNotSerialize]
        // private new int port;
        
        void Awake()
        {
            _parser = new Parser();
        }
        
        new void OnEnable()
        {
            Listener = GetComponent<RTPListener>();
        }
        void Update()
        {
            VRTPData data;

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
            while (!Listener.MocapDataIn.IsEmpty)
            {
                if (WaitForAudio && Listener.MocapDataIn.TryPeek(out data))
                {
                    var dt = DateTime.Now - data.Arrived;
                    // buflength is the buffer size in samples
                    int bufLength;
                    AudioSettings.GetDSPBufferSize(out bufLength, out _);   
                    // divide it by the sample rate (samples / second) to get the amount of time in seconds
                    float delayMs = (float) bufLength / 44000;
                    if (dt.TotalMilliseconds / 1000 < delayMs)
                    {
                        // Debug.Log($"Pausing on mocap packet, it's not ready yet. {dt.TotalMilliseconds / 1000} {delayMs}");
                        // Listener.MocapDataIn.Enqueue(data);  // todo this may put it out of order...
                        break;
                    }
                }
                if (Listener.MocapDataIn.TryDequeue(out data))
                {
                    
                    
                    var pos = 0;
                    // TODO may be worth revisiting this at some point to remove this weird indirection
                    _parser.Parse(data.Payload, ref pos, data.PayloadSize);
                    
                    // loop over everything to clear the queue
                    while (_parser.messageCount > 0)
                    {
                        var message = _parser.Dequeue();
                        // Debug.Log($"OSC Message in: {message}");
                        onDataReceived.Invoke(message);
                        
#if UNITY_EDITOR
                    _onDataReceivedEditor.Invoke(message);
#endif
                    }
                   
                    
                }
                
                
                
            }
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