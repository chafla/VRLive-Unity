using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using RTP;
using UnityEngine;
using UnityEngine.Networking;
using uOSC;

namespace VRLive.Runtime
{
    [RequireComponent(typeof(BackingTrackListener))]
    public class BackingTrackManager : MonoBehaviour
    {
        public BackingTrackListener Listener { get; protected set; }

        public AudioSource source;

        public AudioClip clip;

        private BackingTrackData? _pendingBackingTrack;

        public UserType userType;

        public ServerEventManager serverEventManager;

        public Dictionary<ushort, DateTime> remoteBackingTrackZeroTimes;

        /// <summary>
        /// Users who have a backing track currently playing.
        /// </summary>
        public HashSet<ushort> backingTrackInProgress;

        public bool playOnLoad = false;
        
        // note: we're provided samples, but as an int32 for some reason?
        // at 44.1kHz this is not enough, so we have to use a float

        public float remoteBackingTrackTiming;

        public float localBackingTrackTiming;

        public bool playing => source.isPlaying;

        /// <summary>
        /// The time the current backing track started at.
        /// </summary>
        public DateTime? backingTrackZeroTime;
        
        public float dif;

        public void Awake()
        {
            remoteBackingTrackZeroTimes = new Dictionary<ushort, DateTime>();
            backingTrackInProgress = new HashSet<ushort>();
            Listener = gameObject.GetComponent<BackingTrackListener>();
            source = gameObject.AddComponent<AudioSource>();
            serverEventManager = gameObject.GetComponent<ServerEventManager>();
            source.playOnAwake = true;
            // source.clip = clip;
            // WWW www = new WWW(url);
            // clip = AudioClip.Create()

            if (serverEventManager)
            {
                serverEventManager.OnNewServerEvent += onServerMessage;
            }

            Listener.NewDataAvailable += onNewBackingTrack;
            
            
        }

       

        // We want some users to have different playback delays, particularly so that the mocap packets coming from the 
        // performers can have time to arrive before we try to sync them up with the backing track.
        public float GetPlaybackDelay()
        {
            switch (userType)
            {
                case UserType.Audience:
                    return 3f;
                case UserType.Performer:
                    return 0f;
                default:
                    Debug.LogError("Invalid usertype when calculating playback delay");
                    return -1f;
            }
        }

        /// <summary>
        /// Listener for updated backing track timing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="packet"></param>
        public void OnNewVRTPPacket(object sender, VRTPPacket packet)
        {
            if (packet.backingTrackPosition > 0)
            {
                backingTrackInProgress.Add(packet.UserID);
                return;
            }
            // Debug.Log("aaa");
            if (packet.OSCSize > 0 && backingTrackInProgress.Contains(packet.UserID) && !remoteBackingTrackZeroTimes.ContainsKey(packet.UserID))
            {
                var timestamp = packet.OSC.ExtractTimestamp();
                if (timestamp is { Year: > 1970 })
                {
                    remoteBackingTrackZeroTimes[packet.UserID] = timestamp.Value;
                    Debug.Log($"{packet.UserID} marked their timestamp as {timestamp}");
                }
                   
                // remoteBackingTrackTiming = packet.backingTrackPosition;

                dif = remoteBackingTrackTiming - localBackingTrackTiming;
            }

            
           
        }

        public void UpdateRemoteBackingTrackPosition(ushort userId, DateTime backingPosition)
        {
            remoteBackingTrackZeroTimes[userId] = backingPosition;
        }

        public void UpdateRemoteBackingTrackPosition(VRTPData data)
        {
            remoteBackingTrackZeroTimes[data.UserID] = data.ExtractTimestamp() ?? DateTime.MinValue;
        }

        public void Update()
        {
            if (_pendingBackingTrack != null)
            {
                Stop();
                StartCoroutine(LoadMusic(_pendingBackingTrack.Value.Title));
                _pendingBackingTrack = null;
            }
            localBackingTrackTiming = source.time;
        }

        public void Play()
        {
            if (!source.isPlaying)
            {
                source.Play();
                backingTrackZeroTime = DateTime.Now;
            }
            
        }

        public void Stop()
        {
            source.Stop();
            backingTrackZeroTime = null;
            remoteBackingTrackZeroTimes.Clear();
            backingTrackInProgress.Clear();
            localBackingTrackTiming = 0;
        }

        public void onNewBackingTrack(object ls, ConcurrentQueue<BackingTrackData> queue)
        {
            // queue shouldn't have more than one thing in it
            BackingTrackData data;
            queue.TryDequeue(out data);
            _pendingBackingTrack = data;

        }

        public void onServerMessage(object incoming, Message msg)
        {
            switch (msg.address)
            {
                case "/server/backing/stop":
                    Stop();
                    break;
                case "/server/backing/start":
                    var startPoint = (int)msg.values[0];
                    var delay = GetPlaybackDelay();
                    Debug.Log($"Starting audio track at {startPoint} after {delay} seconds");
                    source.timeSamples = startPoint;
                    
                    Invoke(nameof(Play), delay);
                    break;
                default:
                    // do nothing if this doesn't pertain to us
                    break;
            }
            
        }

        /*
        IEnumerator LoadMusic2(String songPath)
        {
            string url = string.Format("file://{0}", Path.GetFullPath(songPath)); 
            WWW www = new WWW(url);
            yield return www;

            source.clip = www.GetAudioClip(false, false, AudioType.MPEG);
            // songName =  song.clip.name;
            // length = song.clip.length;
            Debug.LogWarning($"Song {source.clip.name} loaded.");

            if (playOnLoad)
            {
                Play();
            }
        }
        */
        
        IEnumerator LoadMusic(string songPath) {
            if(System.IO.File.Exists(songPath))
            {
                // var sysPath = new System.Uri(songPath).AbsoluteUri;
                var path = Path.GetFullPath(songPath);
                using (var uwr = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.UNKNOWN))
                {
                    ((DownloadHandlerAudioClip)uwr.downloadHandler).streamAudio = true;
 
                    yield return uwr.SendWebRequest();
 
                    if (uwr.isNetworkError || uwr.isHttpError)
                    {
                        Debug.LogError(uwr.error);
                        yield break;
                    }
 
                    DownloadHandlerAudioClip dlHandler = (DownloadHandlerAudioClip)uwr.downloadHandler;
 
                    if (dlHandler.isDone)
                    {
                        AudioClip audioClip = dlHandler.audioClip;
 
                        if (audioClip != null)
                        {
                            source.clip = DownloadHandlerAudioClip.GetContent(uwr);
 
                            Debug.Log("Playing song using Audio Source!");
                       
                        }
                        else
                        {
                            Debug.Log("Couldn't find a valid AudioClip :(");
                        }
                    }
                    else
                    {
                        Debug.Log("The download process is not completely finished.");
                    }
                }
            }
            else
            {
                Debug.Log("Unable to locate converted song file.");
            }
        }
    }

    public class RemoteBackingTrackOffset
    {
        public int audioTimestamp;
        public uint backingTrackAudioTimestampOffset;
    }
}