using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        public double dspTime;

        public AudioSource source;
        
        // public List<float[]>

        const int audioClipLength = 1024 * 6;

        private float[] audioClipData;

        private int head;
        
        // public ConcurrentQueue<float[]>

        public Queue<(float[], double)> audioDataUnprocessed;

        public double maxTimeInQueue = 0.05;

        public int unprocessedPressure;
        


        private AudioClip _clip;

        // we need an audiosource for the filterread event to proc
        public AudioSource outputSpeaker;

        public void Awake()
        {
            audioDataUnprocessed = new Queue<(float[], double)>();
            Listener = GetComponent<RTPListener>();
            source = gameObject.AddComponent<AudioSource>();
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
            
            // Listener.OnNewData += (sender, packet) => 
        }

        public void onNewData(object sender, VRTPPacket pkt)
        {
            
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
        
        // https://www.reddit.com/r/Unity3D/comments/ag4cji/how_does_onaudiofilterread_works/
        private void OnAudioFilterRead(float[] audioFilterData, int channelCount)
        {
            dspTime = AudioSettings.dspTime;
            VRTPData data;
            List<(float[], double)> packets = new List<(float[], double)>();
            foreach ((var pkt, var time) in audioDataUnprocessed)
            {
                if (dspTime - time > maxTimeInQueue)
                {
                    continue;
                }
                packets.Add((pkt, time));
            }
            // (audioDataUnprocessed);
            audioDataUnprocessed.Clear();
            float[] pcmOut = new float[960 * Listener.AudioDataIn.Count];
            var startTime = DateTime.Now;
            while (!Listener.AudioDataIn.IsEmpty)
            {
                Listener.AudioDataIn.TryDequeue(out data);
                pcmOut = new float[960];

               
                var dataOut = Decoder.Decode(data.Payload, data.Payload.Length, pcmOut);
                if (dataOut < 0)
                {
                    throw new Exception($"Opus Error {dataOut}");
                }
                
                packets.Add((pcmOut, dspTime));

                
                // OnDecoded(pcmOut, dataOut);
            }
            var endTime = DateTime.Now - startTime;
            // Debug.Log($"Decoding data took {endTime.TotalMilliseconds}");

            var saveForLater = false;
            int filterIx = 0;
            foreach (var packet in packets)
            {
                if (saveForLater)
                {
                    audioDataUnprocessed.Enqueue(packet);
                    continue;
                }
                for (int i = 0; i < packet.Item1.Length; i++)
                {
                    if (saveForLater)
                    {
                        break;
                    }
                    for (int j = 0; j < channelCount; j++)
                    {
                        audioFilterData[filterIx++] = packet.Item1[i];
                        if (filterIx >= audioFilterData.Length - 1)
                        {
                            audioDataUnprocessed.Enqueue((packet.Item1[i..], dspTime));
                            saveForLater = true;
                            break;
                        }
                    }
                }
            }
           

            for (int i = filterIx; i < audioFilterData.Length; i++)
            {
                audioFilterData[i] = 0;
            }
            // double data based on the number of channels: our audio is mono
            // for (int i = 0; i < pcmOut.Length * channelCount && i < audioFilterData.Length; i++)
            // {
            //         for (int j = 0; j < channelCount; j++)
            //         {
            //             audioFilterData[j] = pcmOut[i];
            //         }
            //         
            //     }
            //
            // }
            // FillBuffer(data, channelCount);
        }
        
        

        private void Update()
        {
            unprocessedPressure = audioDataUnprocessed.Count;
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