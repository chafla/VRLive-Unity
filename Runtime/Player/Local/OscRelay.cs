using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using uOSC;
using Thread = System.Threading.Thread;

namespace VRLive.Runtime.Player.Local
{
    public class OscRelay : MonoBehaviour
    {
        public event EventHandler<byte[]> OnNewMessage;

        public int listeningPort;
        public int destPort;
        public string destIP;

        public ConcurrentQueue<byte[]> incomingData;

        private bool _sendActive = false;
        private Thread _sendThread;

        private bool _listenActive = false;
        private Thread _listenThread;
        
        public void Awake()
        {
            incomingData = new ConcurrentQueue<byte[]>();
        }

        public void StartThreads()
        {
            
            _sendThread = new Thread(SendMocapDataThread);
            _listenThread = new Thread(RecvMocapDataThread);
            if (!_sendActive)
                _sendThread.Start();
            if (!_listenActive)
                _listenThread.Start();
        }

        private Thread _dispatchThread;

        public void RecvMocapDataThread()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.ReceiveTimeout = 5000;
            
            // socket.Bind(new IPEndPoint(new IPAddress(0), listeningPort));
            byte[] buf;
            _listenActive = true;
            var lastPort = listeningPort;
            EndPoint currentEndpoint = new IPEndPoint(new IPAddress(0), listeningPort);
            socket.Bind(currentEndpoint);
            while (_listenActive)
            { 
                if (lastPort != listeningPort)
                {
                    currentEndpoint = new IPEndPoint(new IPAddress(0), listeningPort);
                    lastPort = listeningPort;
                    socket.Bind(currentEndpoint);
                }
                buf = new byte[10000];
                int bytesIn;
                try
                {
                    bytesIn = socket.Receive(buf);
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode != SocketError.TimedOut)
                    {
                        Debug.LogException(e);
                        break;
                    }

                    continue;
                }
                
                
                OnNewMessage?.Invoke(this, buf[..bytesIn]);
                incomingData.Enqueue(buf[..bytesIn]);
            }
            
        }
        
        public void SendMocapDataThread()
        {
            // TODO find a way to integrate controllers into this as well
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.ReceiveTimeout = 5000;  // to check to see if we're running or not
            _sendActive = true;
            while (_sendActive)
            {
                // if (lastPort != localMocapToServerPort || lastHost != serverHostIP)
                // {
                //     currentEndpoint = new IPEndPoint(IPAddress.Parse(serverHostIP), localMocapToServerPort);
                //     lastPort = localMocapToServerPort;
                //     lastHost = serverHostIP;
                // }

                // Bundle b = new Bundle();
                // Message ;
                // we have to be careful about this: slimeVR is capable of outputting a LOT of data.
                // This can lead to significant pressure, not on the rust side but on the unity side, trying to keep up with everything.
                // var messagesInCurBundle = 0;
                // var maxMessagesPerBundle = 10;
                var endpoint = new IPEndPoint(IPAddress.Parse(destIP), destPort);
                byte[] data;
                while (incomingData.TryDequeue(out data))
                {
                    // todo if this breaks try sending them individually, but I think bundling them up makes more sense?
                    // Bundle b = new Bundle();
                    socket.SendTo(data, endpoint);
                    // b.Add(msg);
                    //
                    // if (++messagesInCurBundle >= maxMessagesPerBundle)
                    // {
                    //     stream = new MemoryStream();
                    //     b.Write(stream);
                    //     sock.SendTo(stream.GetBuffer(), currentEndpoint);
                    //     
                    //     messagesInCurBundle = 0;
                    //     b = new Bundle();
                    // }
                }
            }
        }
    }
}