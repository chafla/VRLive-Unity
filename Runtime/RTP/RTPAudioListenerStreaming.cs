using System;
using UnityEngine;
using UnityOpus;

namespace RTP
{
    [RequireComponent(typeof(RTPListener))]
    public class RTPAudioListenerStreaming : MonoBehaviour
    {
        public RTPListener Listener;
        public SamplingFrequency sampleRate = SamplingFrequency.Frequency_48000;
        public NumChannels channels = NumChannels.Mono;
        protected Decoder Decoder;

        public bool Started { get; }

        const int audioClipLength = 1024 * 6;

        private float[] audioClipData;

        private int head;
        


        private AudioClip _clip;

        public AudioSource outputSpeaker;

        public void Awake()
        {
            Listener = GetComponent<RTPListener>();
            var dummy = AudioClip.Create ("dummy", 1, 1, AudioSettings.outputSampleRate, false);

            // dummy.SetData(new float[] { 1 }, 0);
            // outputSpeaker.clip = dummy; //just to let unity play the audiosource
            // outputSpeaker.loop = true;
            // outputSpeaker.spatialBlend=1;
            // outputSpeaker.Play ();
            // _clip = AudioClip.Create("RTP audio source", audioClipLength, (int)channels, (int)sampleRate, true, OnAudioRead);
            //
            // outputSpeaker.clip = _clip;
            // outputSpeaker.Play();
        }

        private void Start()
        {
            if (!Listener.Running)
            {
                Listener.StartServer();
            }
            Decoder = new Decoder(sampleRate, channels);

            
        }
        
        private void OnAudioRead(float[] audioFilterData)
        {
            VRTPData data;
            float[] pcmOut = new float[960 * Listener.AudioDataIn.Count];
            while (!Listener.AudioDataIn.IsEmpty)
            {
                Listener.AudioDataIn.TryDequeue(out data);
                pcmOut = new float[960];
                
                var dataOut = Decoder.Decode(data.Payload, data.Payload.Length, pcmOut);
                if (dataOut < 0)
                {
                    throw new Exception($"Opus Error {dataOut}");
                }
                
                // OnDecoded(pcmOut, dataOut);
            }
            
            for (int i = 0; i < audioFilterData.Length; i++)
            {
                if (i >= pcmOut.Length)
                {
                    audioFilterData[i] = 0;
                }
                else
                {
                    audioFilterData[i] = pcmOut[i];
                }
                
                
            }
        }
        
        private void OnAudioFilterRead(float[] audioFilterData, int channelCount)
        {
            VRTPData data;
            float[] pcmOut = new float[960 * Listener.AudioDataIn.Count];
            while (!Listener.AudioDataIn.IsEmpty)
            {
                Listener.AudioDataIn.TryDequeue(out data);
                pcmOut = new float[960];
                
                var dataOut = Decoder.Decode(data.Payload, data.Payload.Length, pcmOut);
                if (dataOut < 0)
                {
                    throw new Exception($"Opus Error {dataOut}");
                }
                
                // OnDecoded(pcmOut, dataOut);
            }
            
            for (int i = 0; i < audioFilterData.Length; i++)
            {
                if (i >= pcmOut.Length)
                {
                    audioFilterData[i] = 0;
                }
                else
                {
                    audioFilterData[i] = pcmOut[i];
                }
                
                
            }
            // FillBuffer(data, channelCount);
        }
        
        

        private void Update()
        {
            // VRTPData data;
            // float[] pcmOut;
            // while (!Listener.AudioDataIn.IsEmpty)
            // {
            //     Listener.AudioDataIn.TryDequeue(out data);
            //     pcmOut = new float[960];
            //     var dataOut = Decoder.Decode(data.Payload, data.Payload.Length, pcmOut);
            //     if (dataOut < 0)
            //     {
            //         throw new Exception($"Opus Error {dataOut}");
            //     }
            //     OnDecoded(pcmOut, dataOut);
            // }
            
        }

        private void OnDisable()
        {
            Listener.Stop();
        }

        private void OnDecoded(float[] pcm, int pcmLength) {
            if (audioClipData == null || audioClipData.Length != pcmLength) {
                // assume that pcmLength will not change.
                audioClipData = new float[pcmLength];
            }
            Array.Copy(pcm, audioClipData, pcmLength);
            outputSpeaker.clip.SetData(audioClipData, head);
            head += pcmLength;
            if (!outputSpeaker.isPlaying && head > audioClipLength / 2) {
                outputSpeaker.Play();
            }
            head %= audioClipLength;
        }
    }
}