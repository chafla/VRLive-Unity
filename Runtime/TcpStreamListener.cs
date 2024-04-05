using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;


/// The idea for this stream listener is that you should be deriving subclasses of it and creating event handlers
/// based on string message titles that come in over the wire.
/// We'll then decode the packets and send things out as needed.
namespace VRLive.Runtime
{
    public abstract class TcpStreamListener<T> : MonoBehaviour
    {
        public int Port;

        private Socket _socket;

        private bool _active = false;

        /// <summary>
        /// Collection of all messages that we are currently hanging onto and haven't yet been asked for
        /// </summary>
        protected ConcurrentQueue<T> messages;

        public string Host;

        private Thread _tcpListenerThread;

        protected delegate T onNewSocketMessage(string messageType, Socket sock, ushort headerLen, uint bodyLen);

        /// <summary>
        /// Mapping of message titles to callbacks invoked when such a message comes in over the wire
        /// </summary>
        protected Dictionary<string, onNewSocketMessage> callbacks;

        // public delegate void onNewDataAvailable();

        public event EventHandler<ConcurrentQueue<T>> NewDataAvailable;

        public bool autoStart = false;

        protected string _tempDir;

        private bool _newMessages = false;

        public TcpStreamListener(int port)
        {
            Port = port;
            _tempDir = FileUtil.GetUniqueTempPathInProject();
            callbacks = new Dictionary<string, onNewSocketMessage>();
        }

        public virtual void Awake()
        {
            _tempDir ??= FileUtil.GetUniqueTempPathInProject();
            callbacks ??= new Dictionary<string, onNewSocketMessage>();
            messages = new();
            
            if (autoStart)
            {
                StartListener();
            }
        }

        public void UpdateConnectionTarget(string addr, ushort port)
        {
            Port = port;
            Host = Host;
        }

        public void Update()
        {
            if (_newMessages)
            {
                _newMessages = false;
                NewDataAvailable?.Invoke(this, messages);
            }
        }

        public void StartListener()
        {
            if (_active) return;
            _active = true;
            _tcpListenerThread = new Thread(RunListener);
            _tcpListenerThread.Start();
        }

        public void OnDisable()
        {
            _active = false;
        }

        public bool HasPendingMessages()
        {
            return !messages.IsEmpty;
        }

        public List<T> GetPendingMessages()
        {
            List<T> messagesOut = new List<T>(messages.Count);
            T messageOut;
            while (!messages.IsEmpty)
            {
                if (!messages.TryDequeue(out messageOut))
                {
                    break;
                }
                
                // todo this may completely break due to pass by ref
                messagesOut.Add(messageOut);
            }

            return messagesOut;

        }

        /*
        private void loadBackingTrack(out byte[] buffer, int length, Socket conn)
        {
            
        }
        */

        private void RunListener()
        {
            
            // _socket ??= new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // _socket.Connect(new IPEndPoint(IPAddress.Parse(Host), Port));
            // _socket.Listen(20);
            // var conn = _socket.Accept();
            byte[] buf;
            string _activeHost = null;
            int _activePort = 0;
            // var invalidCooldownSecs = 5;
            // var lastInvalidCooldown = Time.time;
            while (_active)
            {
                if (_activeHost != Host || _activePort != Port)
                {
                    _socket ??= new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    Debug.Log($"Listening for TCP traffic on {Port}");
                    _socket.Connect(new IPEndPoint(IPAddress.Parse(Host), Port));
                    Debug.Log($"Got connection to {_socket.RemoteEndPoint}");
                    _activeHost = Host;
                    _activePort = Port;
                }
                
                // get the message preamble
                var msgBuf = new byte[1];
                int bytesIn;
                try
                {
                    bytesIn = _socket.Receive(msgBuf);
                }
                catch (SocketException e)
                {
                    Debug.LogException(e);
                    break;
                }
                
                if (bytesIn == 0)
                {
                    Debug.LogWarning("Zero-length message, terminating connection.");
                    break;
                }
                var titleMessageLen = msgBuf[0];
                msgBuf = new byte[titleMessageLen];
                bytesIn = _socket.Receive(msgBuf);
                if (bytesIn == 0)
                {
                    Debug.LogWarning("Zero-length message, terminating connection.");
                    break;
                }

                string messageType = Encoding.UTF8.GetString(msgBuf, 0, titleMessageLen);

                onNewSocketMessage? callback;
                var delegateToCall = callbacks.TryGetValue(messageType, out callback);
                if (!delegateToCall || callback == null)
                {
                    Debug.Log($"Got invalid message {messageType}");
                    Debug.Log(callbacks);
                    continue;
                }
                // then, get the different sizes from our expected headers
                buf = new byte[6];
                
                bytesIn = _socket.Receive(buf);
                if (bytesIn == 0)
                {
                    Debug.Log("Zero-length message, terminating connection.");
                    break;
                }

                // todo dynamically get in header and body and let a delegate handle that
                var headerLen = BinaryPrimitives.ReadUInt16BigEndian(buf[..2]);
                var bodyLen = BinaryPrimitives.ReadUInt32BigEndian(buf[2..6]);
                Debug.Log("new tcp message of length {length}");
                try
                {
                    var data = callback.Invoke(messageType, _socket, headerLen, bodyLen);

                    messages.Enqueue(data);
                    _newMessages = true;

                    // the update is called outside of this threaded method so any callbacks can interact with Unity
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

            }
            
            Debug.LogWarning("Shutting down listener!");
        }
    }


}