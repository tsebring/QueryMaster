
#region License
/*
Copyright (c) 2015 Betson Roy

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

namespace QueryMaster.GameServer
{
    internal class ServerSocket : QueryMasterBase
    {
        internal static readonly int UdpBufferSize = 1400;
        internal static readonly int TcpBufferSize = 4110;
        internal IPEndPoint Address = null;
        protected internal int BufferSize = 0;
        internal EngineType EngineType { get; set; }
        internal Socket Socket { set; get; }
        private readonly object LockObj = new object();

        internal int ReceiveTimeout
        {
            get
            {
                return _receiveTimeout;
            }

            set
            {
                _receiveTimeout = value;
                if (Socket != null) Socket.ReceiveTimeout = value;
            }
        }
        private int _receiveTimeout = 0;

        internal ServerSocket(ConnectionInfo conInfo,ProtocolType  type)
        {
            switch (type)
            {
                case ProtocolType.Tcp: 
                    Socket = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, ProtocolType.Tcp); 
                    BufferSize = TcpBufferSize; 
                    break;
                case ProtocolType.Udp: 
                    Socket = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, ProtocolType.Udp); 
                    BufferSize = UdpBufferSize; 
                    break;
                default: throw new ArgumentException("An invalid SocketType was specified.");
            }

            Socket.SendTimeout = conInfo.SendTimeout;
            ReceiveTimeout = conInfo.ReceiveTimeout;
            Address = conInfo.EndPoint;
            var success = Reconnect();
            if (!success)
                throw new SocketException((int)SocketError.TimedOut);
            IsDisposed = false;
        }

        internal bool Reconnect()
        {
            IAsyncResult result = Socket.BeginConnect(Address, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(ReceiveTimeout, true);

            return success;
        }

        internal int SendData(byte[] data)
        {
            ThrowIfDisposed();
            lock(LockObj)
                return Socket.Send(data);
        }

        internal int Receive(byte[] buffer)
        {
            ThrowIfDisposed();
            var count = Socket.Receive(buffer);
            if (count == 0)
            {
                //if reconnection does not work it could be because new authorization is required.
                Debug.WriteLine("Socket.Receive returned 0, attempting to reconnect...");
                var success = Reconnect();
                if (success) count = Socket.Receive(buffer);
            }

            return count;
        }

        internal byte[] ReceiveData()
        {
            ThrowIfDisposed();
            byte[] recvData = new byte[BufferSize];
            int recv = 0;
            lock(LockObj)
                recv = Socket.Receive(recvData);
            return recvData.Take(recv).ToArray();
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    lock (LockObj)
                    {
                        if (Socket != null)
                            Socket.Close();
                    }
                }
                base.Dispose(disposing);
                IsDisposed = true;
            }
        }
    }
}
