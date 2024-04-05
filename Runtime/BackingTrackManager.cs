using System.Collections;
using System.Collections.Concurrent;
using System.IO;
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

        private ServerEventManager _serverEventManager;

        public bool playOnLoad = false;

        public void Awake()
        {
            Listener = gameObject.GetComponent<BackingTrackListener>();
            source = gameObject.AddComponent<AudioSource>();
            _serverEventManager = gameObject.GetComponent<ServerEventManager>();
            source.playOnAwake = true;
            // source.clip = clip;
            // WWW www = new WWW(url);
            // clip = AudioClip.Create()

            if (_serverEventManager)
            {
                _serverEventManager.OnNewServerEvent += onServerMessage;
            }

            Listener.NewDataAvailable += onNewBackingTrack;
        }

        public void Update()
        {
            if (_pendingBackingTrack != null)
            {
                Stop();
                StartCoroutine(LoadMusic(_pendingBackingTrack.Value.Title));
                _pendingBackingTrack = null;
            }
        }

        public void Play()
        {
            if (!source.isPlaying)
            {
                source.Play();
            }
            
        }

        public void Stop()
        {
            source.Stop();
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
                    source.Stop();
                    break;
                case "/server/backing/start":
                    var startPoint = (int)msg.values[0];
                    source.timeSamples = startPoint;
                    source.Play();
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
}