using System.Net.Sockets;
using UnityEngine;
using uOSC;

namespace VRLive.Runtime
{
    public class ServerEventListener : TcpStreamListener<Message>
    {

        // very annoying that this is a stateful parser but it's from the library and it Works, y'know
        private Parser _oscParser;
        
        // delegate for server messages.
        public delegate void OnServerMessage(object sender, string messageDest, Message msg);

        
        
        public ServerEventListener(int port) : base(port)
        {
        }
        
        public override void Awake()
        {
            base.Awake();
            callbacks["BUNDLE"] = OnSockData;
            _oscParser = new Parser();
        }

        private Message OnSockData(string messageType, Socket conn, ushort headerLen, uint bodyLen)
        {
            // we don't really have anything we need in the header at this point, it's really just 
            // the length of the body we care about

            byte[] messageData = new byte[bodyLen];

            var nBytes = conn.Receive(messageData);

            if (nBytes < bodyLen)
            {
                Debug.LogWarning($"We got fewer bytes for an OSC message than we expected {nBytes} < {bodyLen}");
            }

            int pos = 0;
            // surely this cast can never go wrong (clueless)
            _oscParser.Parse(messageData, ref pos, (int) bodyLen);
            
            

            return _oscParser.Dequeue();
        }

    }
}