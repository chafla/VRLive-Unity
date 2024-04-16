using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityOpus;

namespace RTP
{
    public class RTPAudioPlayer : MonoBehaviour
    {
        public RTPAudioListenerStreaming streamSource;

        public AudioSource source;

        public int userId;

        public int head;

        public int clipHead;

        public RemoteAudioSource sourceDesc;

        public int clipLength;

        public bool superLowLatency = false;
        
        
        public Decoder decoder;

        public void Register(int userId, int clipLength, NumChannels channels, SamplingFrequency sampleRate)
        {
            source = gameObject.AddComponent<AudioSource>();
            var clip = AudioClip.Create($"RTP audio source {userId}", clipLength, (int)channels, (int)sampleRate, false);
            source.clip = clip;
            this.clipLength = clipLength;
            sourceDesc = new RemoteAudioSource(clipLength);
            decoder = new Decoder(sampleRate, channels);

        }

        public void Awake()
        {
            source = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        }

        // public void OnAudioFilterRead(float[] data, int channels)
        // {
        //     sourceDesc?.ReadOutData(ref data);
        // }

        public void AddNewData(float[] data, int length)
        {
            sourceDesc.OnNewData(data, length);
        }

        public void Decode(VRTPData data, out int bytesParsed, out float[] pcmOut)
        {
            pcmOut = new float[48000];
            bytesParsed = decoder.Decode(data.Payload, data.Payload.Length, pcmOut);
        }

        public void OnNewData(VRTPData data)
        {
            // if (audioClipData == null || audioClipData.Length != pcmLength) {
                // assume that pcmLength will not change.
                // audioClipData = new float[pcmLength];
            // }
            // Array.Copy(pcm, audioClipData, pcmLength);

            int bytesParsed;
            float[] pcmOut;
            
            Decode(data, out bytesParsed, out pcmOut);
            
            
            
            source.clip.SetData(pcmOut, superLowLatency ? source.timeSamples : head);
            
            head += bytesParsed;
            if (!source.isPlaying) {
                source.Play();
            }

            // source.timeSamples = head;

            clipHead = source.timeSamples;
            
            // transform.localScale = new Vector3(1, GetRMS(audioClipData) * 100.0f, 1);
            head %= clipLength;
        }
        
        // https://www.reddit.com/r/Unity3D/comments/ag4cji/how_does_onaudiofilterread_works/
    }
    
    [Serializable]
    public class RemoteAudioSource
    {
        [DoNotSerialize]
        public float[] Buffer;
        public int ReadHead = 0;
        public int WriteHead = 0;

        public bool ReadHeadWrappedAround = false;
        public bool WriteHeadWrappedAround = false;

        public RemoteAudioSource(int clipLength)
        {
            Buffer = new float[48000 * 10];
        }
        
        
        
        

        public void ReadOutData(ref float[] targetBuf)
        {
            // TODO we need to handle what happens if the read head outpaces the write head
            if (WriteHead <= ReadHead && !WriteHeadWrappedAround)
            {
                return;
            }
            
            // if (ReadHeadWrappedAround && WriteHeadWrappedAround && ReadHead )
        
            var nBytesToReplace = Math.Min(targetBuf.Length, WriteHead - ReadHead);
            
            Array.Copy(Buffer, ReadHead, targetBuf, 0, nBytesToReplace);
            var existingReadHead = ReadHead;
            ReadHead = (nBytesToReplace + ReadHead) % Buffer.Length;

            if (existingReadHead < ReadHead)
            {
                ReadHeadWrappedAround = true;
            }
        }

        public void OnNewData(float[] newData, int dataLength)
        {
            // Debug.Log((int)(AudioSettings.dspTime * 48000));
            var newDataWriteHead = 0;
            if (dataLength + WriteHead > Buffer.Length)
            {
                var firstHalf = Buffer.Length - WriteHead;
                Array.Copy(newData, WriteHead, Buffer, newDataWriteHead, firstHalf);
                newDataWriteHead = firstHalf;
                WriteHead = 0;
            }
            Array.Copy(newData, newDataWriteHead, Buffer, WriteHead, dataLength - newDataWriteHead);
            var existingWriteHead = WriteHead;
            WriteHead = (WriteHead + dataLength - newDataWriteHead) % Buffer.Length;

            if (existingWriteHead < WriteHead)
            {
                WriteHeadWrappedAround = true;
            }
        }

    }
}