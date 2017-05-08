
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
using System.Net;
using QueryMaster;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace QueryMaster.GameServer
{
    internal class TcpQuery : ServerSocket
    {
        private byte[] EmptyPkt = new byte[] { 0x0a, 0x00, 0x00, 0x00, 0x0A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        private ConnectionInfo _conInfo;
        private SemaphoreSlim _ss = new SemaphoreSlim(1, 1);
        private Task _procTask;
        private delegate void OnPacketEventHandler(byte[] data, Exception ex);
        private event OnPacketEventHandler OnPacket;
        

        internal TcpQuery(ConnectionInfo conInfo)
            : base(conInfo, ProtocolType.Tcp)
        {
            _conInfo = conInfo;
        }

        internal void Init()
        {
            ReceiveTimeout = 0;
            _procTask = Task.Run(() => _proc_callback());
        }

        private void _proc_callback()
        {
            try
            {
                var buffer = new List<byte>();
                byte[] recvData = new byte[BufferSize];
                int size = 0;
                while (true)
                {
                    int count = 0;
                    Exception lastException = null;
                    try
                    {
                        count = Receive(recvData);
                    } catch(Exception ex) { lastException = ex; }

                    if (lastException != null)
                    {
                        OnPacket?.Invoke(null, new QueryMasterException("Receive failed", lastException));
                        break;
                    }

                    if (count > 0)
                    {
                        buffer.AddRange(recvData.Take(count));
                        while (RconUtil.IsPacket(buffer.ToArray(), out size))
                        {
                            var packet = buffer.Take(size).ToArray();
                            OnPacket?.Invoke(packet, null);
                            buffer.RemoveRange(0, size);
                        }
                    }
                    else
                    {
                        OnPacket?.Invoke(null, new QueryMasterException("Receive failed because socket was shutdown by remote host"));
                        break;
                    }
                }
            }
            catch (Exception) { }
        }

        internal byte[] GetResponse(byte[] msg)
        {
            byte[] recvData;
            SendData(msg);
            recvData = ReceiveData();//Response value packet
            //recvData = ReceiveData();//Auth response packet

            return recvData;
        }

        internal async Task<RconSrcPacket> GetResponseAsync(RconSrcPacket senPacket)
        {
            if (_procTask == null) throw new QueryMasterException($"{nameof(TcpQuery)} must be initialized before calling {nameof(GetResponseAsync)}");
            if (_procTask.IsCompleted) throw new QueryMasterException("Receive thread is in a failed state");

            var tcs = new TaskCompletionSource<RconSrcPacket>();
            var handler = new OnPacketEventHandler((data, exception) =>
            {
                if (data != null)
                {
                    var packet = RconUtil.ProcessPacket(data);
                    if (packet.Id == senPacket.Id)
                    {
                        tcs.SetResult(packet);
                    }
                }

                if (exception != null) tcs.SetException(exception);
            });

            try
            {
                await _ss.WaitAsync();
                OnPacket += handler;
                try
                {
                    SendData(RconUtil.GetBytes(senPacket));
                }
                catch (Exception ex) { throw new QueryMasterException("Failed to send packet", ex); }

                if (await Task.WhenAny(tcs.Task, Task.Delay(_conInfo.ReceiveTimeout)) == tcs.Task)
                {
                    return tcs.Task.Result;
                }
                else return null;
            }
            finally
            {
                OnPacket -= handler;
                _ss.Release();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    _ss?.Dispose();
                    _ss = null;
                    _procTask?.Dispose();
                    _procTask = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}