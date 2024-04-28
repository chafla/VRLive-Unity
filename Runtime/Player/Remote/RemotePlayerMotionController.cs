using System;
using System.Collections.Concurrent;
using RTP;
using Unity.VisualScripting;
using UnityEngine;
using uOSC;

namespace VRLive.Runtime.Player
{
    /// <summary>
    /// Manager for an avatar that is simply controlled by motion, manipulated by messages given to its queue.
    /// 
    /// </summary>
    public abstract class PlayerMotionController : MonoBehaviour
    {
        public RemotePlayerManagerBase parent;

        public int userId;

        public ConcurrentQueue<VRTPPacket> messages;

        public int messagePressure;
        
        private Parser _parser;

        public bool ready;

        protected bool checkRawData = false;

        public virtual void Awake()
        {
            _parser = new Parser();
            messages = new ConcurrentQueue<VRTPPacket>();
        }

        public void OnListenerData(object src, VRTPPacket pkt)
        {
            messages.Enqueue(pkt);
        }
        
        public virtual void Update()
        {
            VRTPPacket msg;
            // var now = DateTime.Now;
            messagePressure = messages.Count;
            while (messages.TryDequeue(out msg))
            {
                {
                   
                    var pos = 0;
                    Message res;
                    if (msg.OSCSize > 0)
                    {
                        if (checkRawData)
                        {
                            OnNewRawMocapData(msg.OSC);
                        }

                        else
                        {
                            // parse may add more than one message so make sure we purge em all
                            _parser.Parse(msg.OSCBytes, ref pos, msg.OSCBytes.Length);


                            while ((res = _parser.Dequeue()).address != "") // see Message.none
                            {
                                OnNewMocapData(res);
                            }
                        }
                    }



                    if (msg.AudioSize > 0)
                    {
                        OnNewAudioData(msg.Audio);
                    }
                    
                    
                }
            }
            // var dur = DateTime.Now - now;
            // Debug.Log($"Took {dur.Ticks} to process audience mocap!");
        }


        /// <summary>
        /// Method called when we receive a new message from our queue.
        /// </summary>
        /// <param name="msg"></param>
        protected abstract void OnNewMocapData(Message msg);

        protected abstract void OnNewRawMocapData(VRTPData data);

        /// <summary>
        /// Method called when we get new audio data from our queue.
        /// This may be stubbed out
        /// </summary>
        /// <param name="data"></param>
        protected abstract void OnNewAudioData(VRTPData data);


    }
}