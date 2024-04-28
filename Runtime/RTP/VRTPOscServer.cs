﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.VisualScripting;
using uOSC;
using VRLive.Runtime;
using Debug = UnityEngine.Debug;


namespace RTP
{
    // [RequireComponent(typeof(RTPListener))]
    public class VRTPOscServer : uOscServer
    {
        
        // public RTPListener Listener;
        public bool WaitForAudio = false;
        private Parser _parser;
        public ConcurrentQueue<VRTPData> mocapDataIn;

        /// <summary>
        /// If true, mocap will wait until it reaches the correct backing track timing
        /// </summary>
        public bool waitForBackingTrack;

        public BackingTrackManager backingManager;

        /// <summary>
        /// Mocap data that is currently waiting the backing track to catch up.
        /// </summary>
        public Queue<(float, Message, ushort)> queuedMocap;

        public int filterNPackets = 0;

        public int maxPacketsPerFrame = 500;

        public int currentMocapPressure;

        public int backingTrackQueuedMocapPressure;

        private DateTime lastMessage = DateTime.MinValue;

        public float audioDelayFactor;

        public int purgeMocapPressureIfGreaterThan = int.MaxValue;

        private bool _active = false;
        
        public Dictionary<string, string> mappings = new Dictionary<string, string>();
        // [DoNotSerialize]
        // private new int port;
        
        void Awake()
        {
            _parser = new Parser();
            queuedMocap = new Queue<(float, Message, ushort)>();
#if OSC_PURGE_AGGRESSIVE
            Debug.LogWarning("The purge is aggressive! Mocap performance will be limited.");
            purgeMocapPressureIfGreaterThan = 10;
            maxPacketsPerFrame = 2;
            filterNPackets = 3;
#endif
        }

        void buildStringMappings()
        {
            mappings.Add("head", "Head");
            mappings.Add("1", "Spine");
            mappings.Add("4", "LeftAnkle");
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
                Debug.LogError($"OSC server on {gameObject} not connected to any listener on startup!");
            }
            _active = true;
        }

        void OnDisable()
        {
            _active = false;
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
            var packetsRead = 0;

            var frameStartTime = DateTime.Now;
            
            // Debug.Log($"Started parsing mocap data at {Time.time}");

            Queue<(float, Message, ushort)> nextFrameQueuedMocap = new Queue<(float, Message, ushort)>();
            

            while (_active && queuedMocap.Count != 0)
            {
                (float, Message, ushort) res;
                Message queuedMessage;
                // float messageBackingTimestamp;
                while (queuedMocap.TryDequeue(out res))
                {
                    queuedMessage = res.Item2;
                    // messageBackingTimestamp = res.Item1;
                    backingManager.remoteBackingTrackZeroTimes.TryGetValue(res.Item3, out var zeroTime);
                    var delay = queuedMessage.timestamp.ToUtcTime() - zeroTime;
                    if (delay.TotalSeconds <= backingManager.localBackingTrackTiming + audioDelayFactor)
                    {
                        onDataReceived.Invoke(queuedMessage);
                    }

                    else
                    {
                        nextFrameQueuedMocap.Enqueue(res);
                    }
                }
            }
            
            // Debug.Log($"Incoming mocap pressure: {Listener.MocapDataIn.Count}");
            while (_active && !mocapDataIn.IsEmpty && packetsRead++ < maxPacketsPerFrame)
            {
                Message queuedMessage;
                float messageBackingTimestamp;
                
               
                // if (filterNPackets > 0 && packetsRead % filterNPackets == 0)
                // {
                //     continue;
                // }
                // // Debug.Log($"Started parsing mocap data with {mocapDataIn.Count} mocap items left");
                // // if (filterNPackets != 0 && packetsRead % filterNPackets != 0)
                // // {
                // //     continue;
                // // }
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
                if (mocapDataIn.TryDequeue(out data))
                {
                    var pos = 0;
                    // TODO may be worth revisiting this at some point to remove this weird indirection
                    _parser.Parse(data.Payload, ref pos, data.PayloadSize);
                    
                    // loop over everything to clear the queue
                    while (_parser.messageCount > 0)
                    {
                        var message = _parser.Dequeue();
                        // we keep track of the first time we saw a packet from this user while the backing track was playing, which means
                        float backingTrackZeroTime;

                        DateTime zeroTime;
                        if (waitForBackingTrack && backingManager && backingManager.playing && backingManager.remoteBackingTrackZeroTimes.TryGetValue(data.UserID, out zeroTime))
                        {

                            // this represents the time offset for the packet that is just coming in
                            var delay = message.timestamp.ToUtcTime() - zeroTime;
                            // var timeToPlay = backingManager.backingTrackZeroTime.Value.AddSeconds();
                            // how long it's been
                            // var dt = DateTime.Now - backingManager.backingTrackZeroTime;
                            // var dt = backingManager.localBackingTrackTiming;
                            // var essageTime = (DateTime.Now - message.timestamp.ToLocalTime()).TotalSeconds;
                            // Debug.Log($"{timeToPlay - DateTime.Now}");
                            Debug.Log($"{delay}, {backingManager.localBackingTrackTiming}");
                            if (delay.TotalSeconds > backingManager.localBackingTrackTiming + audioDelayFactor)
                            {
                                nextFrameQueuedMocap.Enqueue(((float) delay.TotalSeconds, message, data.UserID));
                                continue;
                            }
                            // var dt = frameStartTime - messageTime;

                        }
                        // Debug.Log($"OSC Message in: {message}");
                        // if ()
                        // var message.timestamp.ToLocalTime()
                        onDataReceived.Invoke(message);
                        
#if UNITY_EDITOR
                    _onDataReceivedEditor.Invoke(message);
#endif
                    }
                   
                    
                }
                
                
                
            }
            
            // Debug.Log($"Finished parsing mocap data at {Time.time}");

            #if DEBUG_MOCAP_PRESSURE
            if (packetsRead >= maxPacketsPerFrame)
            {
                Debug.LogWarning($"Current mocap pressure after parsing: {mocapDataIn.Count}");
            }
            #endif

            currentMocapPressure = mocapDataIn.Count;
            queuedMocap = nextFrameQueuedMocap;
            backingTrackQueuedMocapPressure = queuedMocap.Count;

            if ((DateTime.Now - lastMessage).TotalSeconds >= 10)
            {
                Debug.Log($"Current pressure: backing track queued: {backingTrackQueuedMocapPressure}, main mocap pressure: {currentMocapPressure}");
                lastMessage = DateTime.Now;
            }
            if (mocapDataIn.Count > purgeMocapPressureIfGreaterThan)
            {
                mocapDataIn.Clear();
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