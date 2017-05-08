
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
using System.Threading.Tasks;
using QueryMaster;
namespace QueryMaster.GameServer
{
    class RconSource : Rcon
    {
        private TcpQuery _socket;
        private ConnectionInfo _connInfo;
        private string _password;

        private RconSource(ConnectionInfo conInfo, string password)
        {
            _connInfo = conInfo;
            _password = password;
        }

        internal static Rcon CreateRconConnection(ConnectionInfo conInfo, string msg)
        {
            return new QueryMasterBase().Invoke<Rcon>(() =>
                {
                    RconSource rcon = null;
                    try
                    {
                        rcon = new RconSource(conInfo, msg);
                        if (!rcon.Reconnect()) throw new QueryMasterException("Failed to connect");

                        return rcon;
                    }
                    catch (Exception ex)
                    {
                        rcon?.Dispose();
                        rcon = null;
                        throw;
                    }
                }, conInfo.Retries + 1, null, conInfo.ThrowExceptions);
        }

        private bool Reconnect()
        {
            if (_socket != null)
            {
                try
                {
                    _socket.Dispose();
                    _socket = null;
                }
                catch { }
            }

            _socket = new TcpQuery(_connInfo);

            //attempt to authorize this conenction
            var packet = new RconSrcPacket() { Body = _password, Id = (int)PacketId.ExecCmd, Type = (int)PacketType.Auth };
            var buffer = _socket.GetResponse(RconUtil.GetBytes(packet));

            if (buffer == null || buffer.Length < 4) return false;

            var header = BitConverter.ToInt32(buffer, 4);
            if (header == -1) return false;

            _socket.Init();
            return true;
        }

        public override async Task<string> SendCommandAsync(string command)
        {
            ThrowIfDisposed();
            return await InvokeAsync<string>(async () => await sendCommandAsync(command), 1, null, _connInfo.ThrowExceptions);
        }

        private async Task<string> sendCommandAsync(string command)
        {
            var send = new RconSrcPacket() { Body = command, Id = (int)PacketId.ExecCmd, Type = (int)PacketType.Exec };
            Exception lastException = null;
            try
            {
                var result = await _socket.GetResponseAsync(send);
                return result?.Body;
            }
            catch (Exception ex) { lastException = ex; }

            if (!Reconnect()) throw new QueryMasterException("Send command and reconnect failed", lastException);

            var resultSecond = await _socket.GetResponseAsync(send);
            return resultSecond?.Body;
        }

        #region Old Code
        public override void AddlogAddress(string ip, ushort port)
        {
            throw new NotSupportedException("Method is deprecated");

            ThrowIfDisposed();
            SendCommand("logaddress_add " + ip + ":" + port);
        }

        public override void RemovelogAddress(string ip, ushort port)
        {
            throw new NotSupportedException("Method is deprecated");

            ThrowIfDisposed();
            SendCommand("logaddress_del " + ip + ":" + port);
        }

        public override string SendCommand(string command, bool isMultipacketResponse = false)
        {
            throw new NotSupportedException("Method is deprecated");
        }

        private string sendCommand(string command, bool isMultipacketResponse)
        {
            throw new NotSupportedException("Method is deprecated");
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    _socket?.Dispose();
                    _socket = null;
                }
                base.Dispose(disposing);
                IsDisposed = true;
            }
        }
    }
}
