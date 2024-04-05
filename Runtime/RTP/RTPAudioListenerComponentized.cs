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

        const int audioClipLength = 1024 * 6;

        private float[] audioClipData;

        private int head;

        public Dictionary<ushort, RemoteAudioSource> AudioSources;
        


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
            while (!Listener.AudioDataIn.IsEmpty)
            {
                Listener.AudioDataIn.TryDequeue(out data);
                pcmOut = new float[960];
                var dataOut = Decoder.Decode(data.Payload, data.Payload.Length, pcmOut);
                if (dataOut < 0)
                {
                    throw new Exception($"Opus Error {dataOut}");
                }
                OnDecoded(pcmOut, dataOut, data.UserID);
            }
            
        }

        private void OnDisable()
        {
            Listener.Stop();
        }

        private void OnDecoded(float[] pcm, int pcmLength, ushort userID) {
            if (audioClipData == null || audioClipData.Length != pcmLength) {
                // assume that pcmLength will not change.
                audioClipData = new float[pcmLength];
            }
            Array.Copy(pcm, audioClipData, pcmLength);
            RemoteAudioSource src;
            if (!AudioSources.TryGetValue(userID, out src))
            {
                var clip = AudioClip.Create($"RTP audio source {userID}", audioClipLength, (int)channels, (int)sampleRate, false);
                var newSource = gameObject.AddComponent<AudioSource>();
                // newSource. = $"Audio source {userID}";
                newSource.clip = clip;
                src = new RemoteAudioSource(userID, newSource);
                AudioSources[userID] = src;
               
            }

            var outputSpeaker = src.Source;
            outputSpeaker.clip.SetData(audioClipData, head);
            head += pcmLength;
            if (!outputSpeaker.isPlaying && head > audioClipLength / 2) {
                outputSpeaker.Play();
            }
            
            transform.localScale = new Vector3(1, GetRMS(audioClipData) * 100.0f, 1);
            head %= audioClipLength;
        }
        
        public float GetRMS(float[] buf) {
            float sum = 0.0f;
            foreach (var sample in buf) {
                sum += sample * sample;
            }
            return Mathf.Sqrt(sum / buf.Length);
        }
    }

    public struct RemoteAudioSource
    {
        public AudioSource Source;
        public ushort UserID;

        public RemoteAudioSource(ushort userID, AudioSource source)
        {
            Source = source;
            UserID = userID;
        }
        
        public RemoteAudioSource(ushort userID)
        {
            Source = new AudioSource();
            UserID = userID;
        }
        
        
    }
    
    
    
}