﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using RTP;
using UnityEngine;
using uOSC;
using Thread = System.Threading.Thread;

namespace VRLive.Runtime.Player.Local
{
    public class OscRelay : MonoBehaviour
    {
        public event EventHandler<VRTPData> OnNewMessage;

        public int listeningPort;
        public int destPort;
        public string destIP;

        public ConcurrentQueue<byte[]> incomingData;

        private bool _sendActive = false;
        private Thread _sendThread;

        private bool _listenActive = false;
        private Thread _listenThread;

        public Transform rootTransform;
        
        private static byte[] _bundleIntro = Encoding.UTF8.GetBytes("#bundle");

        /// <summary>
        /// If true, the timestamp of transmission will be marked as the true "mocap timestamp".
        /// This should be a relatively inexpensive operation, as it requires little parsing.
        /// </summary>
        public bool shouldInjectTimestamp = true;
        
        
        
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

                var vrtpData = new VRTPData((ushort) bytesIn, buf[..bytesIn], 0);
                OnNewMessage?.Invoke(this, vrtpData);
                incomingData.Enqueue(buf[..bytesIn]);
            }
            
        }

        /// <summary>
        /// Inject a timestamp into the first (and hopefully top-level) OSC bundle.
        /// </summary>
        public void injectTimestamp(byte[] data)
        {
            // how far to look for our bundle before giving up
            var maxBytesToSearch = 50;
            var noTimestamp = true;
            var noInnerTimestamp = false;
            for (int i = 0; i < data.Length - _bundleIntro.Length && i < maxBytesToSearch; i++)
            {
                for (int j = 0; j < _bundleIntro.Length; j++)
                {
                    if (data[i + j] != _bundleIntro[j])
                    {
                        noInnerTimestamp = true;
                        break;
                    }
                }

                if (noInnerTimestamp)
                {
                    continue;
                }
                
                
                // get the starting index of our timetag
                // it starts one character after the bundle intro
                var timetagPos = i + _bundleIntro.Length + 1;
                // 64 big-endian fixed point time tag
                // first 32 bits are for the epoch seconds
                // last 32 bits are for fractional seconds (2<<32 would technically be 1.0)

                // var curTime = DateTime.Now;
                
                
                // https://stackoverflow.com/a/21055459
                // this method gets us fractional seconds as well
                // also note that according to the spec it's time since 1/1/1900
                var timeSpan = DateTime.UtcNow - new DateTime(1900, 1, 1, 0, 0, 0);
                var timeSeconds = timeSpan.TotalSeconds;
                var timeSecsTrunc = (uint)timeSeconds;  // this may lose some precision after 2038 be warned
                var fracSecs = timeSeconds - timeSecsTrunc;
                var fracSecsTotal = fracSecs * (0x100000000);

                var fracSecsBytes = BitConverter.GetBytes((uint)fracSecsTotal);
                var totalSecsBytes = BitConverter.GetBytes(timeSecsTrunc);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(fracSecsBytes);
                    Array.Reverse(totalSecsBytes);
                }
                
                Array.Copy(totalSecsBytes, 0, data, timetagPos, totalSecsBytes.Length);
                Array.Copy(fracSecsBytes, 0, data, timetagPos + 4, fracSecsBytes.Length);
                return;


            }
            
            Debug.LogWarning("Could not find a #bundle tag in parsed OSC message.");
            
        }

        /// <summary>
        /// SlimeVR doesn't transfer HMD info, like the head.
        /// As a result, we need to make sure we pass that along.
        /// </summary>
        public void InjectRootTransform()
        {
            
        }
        
        public void SendMocapDataThread()
        {
            // TODO find a way to integrate controllers into this as well
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.ReceiveTimeout = 5000;  // to check to see if we're running or not
            _sendActive = true;
            while (_sendActive)
            {
                var endpoint = new IPEndPoint(IPAddress.Parse(destIP), destPort);
                byte[] data;
                while (incomingData.TryDequeue(out data))
                {
                    if (shouldInjectTimestamp)
                    {
                       injectTimestamp(data);
                    }
                    
                    // we don't need to fu
                    
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