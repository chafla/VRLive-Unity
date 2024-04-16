using System;
using System.Collections.Generic;
using UnityEngine;
using UnityOpus;

namespace RTP
{
    [RequireComponent(typeof(RTPListener))]
    public class RTPAudioListenerComponentized : MonoBehaviour
    {
        public RTPListener Listener;
        public SamplingFrequency sampleRate = SamplingFrequency.Frequency_48000;
        public NumChannels channels = NumChannels.Mono;
        protected Decoder Decoder;

        public bool Started { get; }

        // const int audioClipLength = 960 * 6
        int audioClipLength => 5 * (int) sampleRate;

        // private float[] audioClipData;
        
        public Dictionary<ushort, RTPAudioPlayer> AudioSources;

        public List<VRTPData> audioBuffer;
        
        private AudioClip _clip;

        // public AudioSource outputSpeaker;

        public void Awake()
        {
            Listener = GetComponent<RTPListener>();
            AudioSources = new();
            int bufferLength, numBuffers;

            AudioSettings.GetDSPBufferSize(out bufferLength, out numBuffers);
            
            Decoder = new Decoder(sampleRate, channels);
            
            Debug.LogWarning($"DSP settings: buffer length: {bufferLength}, num buffers: {numBuffers}");
        }

        private void Start()
        {
            if (!Listener.Running)
            {
                Listener.StartServer();
            }
           

            // _clip = AudioClip.Create("RTP audio source", audioClipLength, (int)channels, (int)sampleRate, false);
            //
            // outputSpeaker.clip = _clip;
        }
        
        

        private void Update()
        {
            VRTPData data;
            float[] pcmOut;
            while (Listener.AudioDataIn.TryDequeue(out data))
            {
                // pcmOut = new float[48000];
                // var dataOut = Decoder.Decode(data.Payload, data.Payload.Length, pcmOut);
                // if (dataOut < 0)
                // {
                //     throw new Exception($"Opus Error {dataOut}");
                // }
                OnNewData(data);
            }
            
        }

        private void OnDisable()
        {
            Listener.Stop();
        }

        private void OnAudioRead(float[] data)
        {
            
        }

        private void OnNewData(VRTPData data) {
            // if (audioClipData == null || audioClipData.Length != pcmLength) {
            //     // assume that pcmLength will not change.
            //     audioClipData = new float[pcmLength];
            // }
            
            
            
            // Array.Copy(pcm, audioClipData, pcmLength);
            RTPAudioPlayer src;
            if (!AudioSources.TryGetValue(data.UserID, out src))
            {
                // var newSource = gameObject.AddComponent<AudioSource>();
                // newSource. = $"Audio source {userID}";

                var newObj = new GameObject();
                newObj.transform.parent = gameObject.transform;
                newObj.name = $"Audio source for {data.UserID}";
                newObj.SetActive(true);
                
                // newSource.loop = true;
                src = newObj.AddComponent<RTPAudioPlayer>();
                AudioSources[data.UserID] = src;
                Debug.Log($"Created new audio source for remote performer {data.UserID}");
                
                src.Register(data.UserID, audioClipLength, channels, sampleRate);
                
                // var clip = AudioClip.Create($"RTP audio source {userID}", audioClipLength, (int)channels, (int)sampleRate, false);
                //
                // newSource.clip = clip;
            }
            
            src.OnNewData(data);

            
        }
        
        public float GetRMS(float[] buf) {
            float sum = 0.0f;
            foreach (var sample in buf) {
                sum += sample * sample;
            }
            return Mathf.Sqrt(sum / buf.Length);
        }
    }

    
    
    
    
}