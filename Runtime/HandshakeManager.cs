using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;

namespace VRLive.Runtime
{
    public class HandshakeManager
    {
        protected ClientPortMap LocalPorts;
        protected IPEndPoint ServerEndpoint;
        protected Thread HandshakeThread;
        protected UserType UserType;
        protected string Identifier;

        private bool _active = false;
        
        public event EventHandler<HandshakeResult> OnHandshakeCompletion; 

        public HandshakeManager(IPEndPoint serverEp, ClientPortMap localPorts, UserType userType, string identifier)
        {
            LocalPorts = localPorts;
            ServerEndpoint = serverEp;
            UserType = userType;
            Identifier = identifier;
        }

        public void RunHandshake()
        {
            if (_active)
            {
                return;
            }
            HandshakeThread = new Thread(RunHandshakeThread);
            HandshakeThread.Start();
        }

        /// <summary>
        /// Run the handshake thread.
        /// This should be ready to spin back up if the server dies.
        /// </summary>
        private void RunHandshakeThread()
        {
            _active = true;

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // Debug.Log($"Listening on {Port}");
            
            // while (_active)
            // {
                socket.Connect(ServerEndpoint);

                Debug.LogWarning($"Established new handshake connection with {ServerEndpoint}");
                var buf = new byte[1024];
                var bytesIn = socket.Receive(buf);
                if (bytesIn == 0)
                {
                    throw new Exception("Handshake was aborted during synack.");
                }
                
                var bufDecoded = Encoding.UTF8.GetString(buf[..bytesIn]);
                var syn = JsonUtility.FromJson<HandshakeSyn>(bufDecoded);
                
                // build our response
                var handshakeSynAck = new HandshakeSynack(UserType, syn.user_id, LocalPorts, Identifier, null);

                var synackOut = JsonUtility.ToJson(handshakeSynAck);
                // send it
                socket.Send(Encoding.UTF8.GetBytes(synackOut));
                
                // then capture the final result

                buf = new byte[1024];
                bytesIn = socket.Receive(buf);
                
                if (bytesIn == 0)
                {
                    throw new Exception("Handshake was aborted after completion.");
                }
                
                bufDecoded = Encoding.UTF8.GetString(buf[..bytesIn]);
                var handshakeComp = JsonUtility.FromJson<HandshakeCompletion>(bufDecoded);

                var result = new HandshakeResult(syn, handshakeComp);
                Debug.LogWarning("Handshake completed successfully.");
                
                OnHandshakeCompletion?.Invoke(this, result);
                

            // }
            _active = false;
        }
        
    }
    
    /// <summary>
    /// Collect all of the useful data that we gathered during the handshake.
    /// </summary>
    public class HandshakeResult
    {
        public ushort userId;
        public string serverIdentifier;
        public ServerPortMap serverPorts;
        public Dictionary<string, ushort> ExtraServerPorts;
        public List<AdditionalUser> OtherUsers;
        
        public HandshakeResult(HandshakeSyn syn, HandshakeCompletion comp)
        {
            userId = syn.user_id;
            serverIdentifier = syn.server_identifier;
            serverPorts = syn.server_ports;
            ExtraServerPorts = comp.extra_ports;
            OtherUsers = comp.other_users;
        }
    }

    [Serializable]
    public struct HandshakeCompletion
    {
        public Dictionary<string, ushort> extra_ports;
        public List<AdditionalUser> other_users;
    }

    [Serializable]
    public struct AdditionalUser
    {
        public UserType user_type;
        public int user_id;
    }

   
    [Serializable]
    public class HandshakeSyn
    {
        public ushort user_id;
        public string server_identifier;
        public ServerPortMap server_ports;
    }
    

    [Serializable]
    public struct HandshakeAck
    {
        public UserType user_id;
        public string server_pretty_identifier;
        public ServerPortMap ports;
    }

    [Serializable]
    public struct HandshakeSynack
    {
        public UserType user_type;
        public ushort user_id;
        public string own_identifier;
        public List<string> user_flags;
        public ClientPortMap ports;
        

        public HandshakeSynack(UserType userType, ushort userId, ClientPortMap clientPorts, string ourIdentifier, [CanBeNull] List<string> userFlags)
        {
            user_type = userType;
            ports = clientPorts;
            own_identifier = ourIdentifier;
            user_flags = userFlags;
            user_id = userId;
        }
    }

    [Serializable]
    public class ClientPortMap
    {
        public ushort backing_track = 6100;
        public ushort server_event = 6101;
        public ushort audience_motion_capture = 6102;
        public ushort vrtp_data = 6105;
        public ExtraPorts extra_ports;
    }
    
    // empty struct is needed here to fill it in and make it serialize
    [Serializable]
    public struct ExtraPorts {}

    [Serializable]
    public class ServerPortMap
    {
        public ushort new_connections = 5653;
        public ushort performer_mocap_in = 5654;
        public ushort performer_audio_in = 5655;
        public ushort client_event_conn_port = 5656;
        public ushort backing_track_conn_port = 5657;
        public ushort server_event_conn_port = 5658;
        public ushort audience_mocap_in = 9000;
    }

}