using RTP;
using UnityEngine;
using uOSC;

namespace VRLive.Runtime.Player.Local
{
    public abstract class LocalPlayerMotionController : MonoBehaviour
    {
        public LocalPlayerManager manager;
        
        /// <summary>
        /// Our OSC relay. Should be set on handshake.
        /// </summary>
        public OscRelay oscRelay;
        
        protected Parser OscParser = new Parser();

        protected bool hasHandshaked;

        public abstract void OnNewRelayMessage(object _, VRTPData data);
        
        public virtual void OnHandshake()
        {
            oscRelay.OnNewMessage += OnNewRelayMessage;
            hasHandshaked = true;
        }

        
        public virtual void Update()
        {
            if (!hasHandshaked && manager && manager.hasHandshaked)
            {
                OnHandshake();
                hasHandshaked = true;
            }
        }
        
    }
}