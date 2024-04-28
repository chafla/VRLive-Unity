using System;
using System.Buffers.Binary;
using System.IO;
using System.Net.Sockets;
using UnityEngine;

namespace VRLive.Runtime
{
    public class BackingTrackListener : TcpStreamListener<BackingTrackData>
    {
        
        public BackingTrackListener(int port) : base(port)
        {
            
        }


        public override void Awake()
        {
            base.Awake();
            callbacks["NEWTRACK"] = OnSockData;
            CleanUpExistingBackingTracks();
        }
        
        /// <summary>
        /// Clean up any existing backing track files
        /// </summary>
        public void CleanUpExistingBackingTracks()
        {
            foreach (var file in System.IO.Directory.GetFiles(_tempDir, "backing*"))
            {
                File.Delete(file);
                Debug.Log($"Deleting existing backing track {file}");
            }
        }

        private BackingTrackData OnSockData(string messageType, Socket conn, ushort headerLen, uint bodyLen)
        {
            int bytesIn;
            var remainingHeaderLength = headerLen - 4;  // subtract body len
            // the remaining data should just be the length of the title
            var buf = new byte[2];
            conn.Receive(buf);
            var titleLen = BinaryPrimitives.ReadUInt16BigEndian(buf);
            // read in the title
            buf = new byte[titleLen];
            conn.Receive(buf);
            var title = System.Text.Encoding.UTF8.GetString(buf);
            Debug.LogWarning($"New backing track incoming: {title}, {bodyLen} bytes.");

            // var fileName = FileUtil.GetUniqueTempPathInProject();
            // var fileName = Path.Join(_tempDir, title);
            var fileName = Path.Join(_tempDir, ($"backing_{ DateTime.Now.Millisecond + title}"));
            
            // var fp = File.OpenWrite(fileName);
            
            // read in the whole track
            
            var bytesRead = 0;

            var outBuf = new byte[bodyLen];

            buf = outBuf;
            
            // bytesIn = conn.Receive(outBuf);
            
            while (bytesRead < bodyLen)
            {
                
                var bytesToRead = Math.Min(bodyLen, bodyLen - bytesRead);
                buf = new byte[bytesToRead];
                bytesIn = conn.Receive(buf);
                if (bytesIn == 0)
                {
                    Debug.Log("Zero-length message, terminating connection.");
                    throw new Exception("Zero length message");
                }
            
                
                Array.Copy(buf, 0, outBuf, bytesRead, bytesIn);
                
                bytesRead += bytesIn;
            
            }
            
            File.WriteAllBytes(fileName, outBuf);
            
            var data = new BackingTrackData(fileName, outBuf);
                
            Debug.LogWarning($"new tcp message of length {outBuf.Length} written out to {fileName}");
            
            return data;

        }
        
        
    }
    
    public struct BackingTrackData
    {
        public string Title;
        public byte[] Data;

        public BackingTrackData(string title, byte[] data)
        {
            Title = title;
            Data = data;
        }
    }
    
}