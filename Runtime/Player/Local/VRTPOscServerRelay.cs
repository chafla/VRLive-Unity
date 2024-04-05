using System;
using Unity.VisualScripting;
using UnityEngine;
using uOSC;

namespace RTP
{
    /// <summary>
    /// Yet another version of the osc server that functions both like the other one, as well as being a relay.
    /// </summary>
    [RequireComponent(typeof(RTPListener))]
    public class VRTPOscServerRelay : uOscServer
    {
        public RTPListener Listener;
        public bool WaitForAudio = false;
        private Parser _parser;

        private bool _active = false;

        // [DoNotSerialize]
        // private new int port;
        
        void Awake()
        {
            _parser = new Parser();
        }
        
        void OnEnable()
        {
            Listener = GetComponent<RTPListener>();
            _active = true;
        }

        void OnDisable()
        {
            _active = false;
        }
        void Update()
        {
            VRTPData data;

            
            var maxPerFrame = 500;
            var packetsRead = 0;
            // Debug.Log($"Incoming mocap pressure: {Listener.MocapDataIn.Count}");
            while (_active && !Listener.MocapDataIn.IsEmpty && packetsRead++ < maxPerFrame)
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

            if (packetsRead >= maxPerFrame)
            {
                Debug.LogWarning($"Current mocap pressure after parsing: {Listener.MocapDataIn.Count}");
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