using System;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;


namespace UnityGameServer.Networking
{
    public class SocketUser
    {
        public const int BufferSize = 1024;

        public IUser User;
        public string SessionToken { get; set; }
        public int Permission { get; set; }
        public bool Connected { get; set; }
        public bool IsAuthenticated { get; set; }
        public bool Receiving { get; private set; }
        public bool UdpEnabled { get; set; }
        public int UdpID { get; set; }
        public bool CloseMessage { get; set; }
        public TcpClient Client { get; set; }
        public System.Security.Cryptography.RSAParameters RSAKeyInfo { get; private set; }

        public IPEndPoint TcpEndPoint { get; set; }
        public IPEndPoint UdpEndPoint { get; set; }

        private Stopwatch timeOutWatch;

        
        private NetworkStream _stream;

        private AsyncServer _server;

        private CancellationTokenSource cts;
        private CancellationToken token;

        public SocketUser(AsyncServer server, TcpClient client, IPEndPoint endPoint)
        {
            _server = server;
            Client = client;
            _stream = client.GetStream();
            SessionToken = HashHelper.RandomKey(8);
            TcpEndPoint = endPoint;
            UdpEndPoint = endPoint;
            timeOutWatch = new Stopwatch();
            timeOutWatch.Start();
            UdpID = -1;
            Permission = 0;
            Connected = true;
            IsAuthenticated = false;
            UdpEnabled = false;
            cts = new CancellationTokenSource();
            token = cts.Token;
        }

        public async Task<bool> HandleStartConnect()
        {
            try
            {
                byte[] message = await _stream.ReadMessage();
                if (message == null)
                {
                    Close(false, "login message null!");
                    return false;
                }

                return (message.Length == 2 && message[0] == 0xff && message[1] == 0x01);
            }
            catch (Exception ex)
            {
                Logger.LogError("{0}: {1}\n{2}", ex.GetType(), ex.Message, ex.StackTrace);
            }
            return false;
        }

        public async Task HandleMessages()
        {
            string closeMessage = string.Empty;
            try
            {
                Task<byte[]> readTask = _stream.ReadMessage();
                while (Connected && !token.IsCancellationRequested && !_server.Token.IsCancellationRequested)
                {
                    await Task.WhenAny(readTask);
                    if (readTask != null && readTask.IsCompleted)
                    {
                        byte[] message = readTask.GetAwaiter().GetResult();
                        if (message == null)
                        {
                            closeMessage = "Connection closed by peer.";
                            return; // client closed.
                        }
                        //User.SessionTimerReset();
                        ProcessReceiveBuffer(message, Protocal.Tcp);
                        readTask = _stream.ReadMessage();
                    }
                    else
                        break;
                }
            }
            catch (IOException)
            {
                // nothing, just close in finally.
            }
            catch (ObjectDisposedException)
            {
                // nothing, just close in finally.
            }
            catch (Exception ex)
            {
                Logger.LogError("{0}: {1}\n{2}", ex.GetType(), ex.Message, ex.StackTrace);
            }
            finally
            {
                Close(false, closeMessage);
            }
        }

        public void EnableUdp(int port)
        {
            UdpEndPoint.Port = port;
            //Logger.Log("UDP end point: {0}:{1}", udpEndPoint.Address.ToString(), udpEndPoint.Port);
            UdpEnabled = true;
        }

        public void SetUser(IUser user)
        {
            User = user;
            if (User != null)
                User.SetSocket(this);
        }

        public void Send(byte command, string message, Protocal type = Protocal.Tcp)
        {
            Send(command, Encoding.UTF8.GetBytes(message), type);
        }

        public void Send(byte cmd, byte[] data, Protocal type = Protocal.Tcp)
        {
            Send(BufferUtils.AddFirst(cmd, data), type);
        }

        public void Send(byte[] data, Protocal type = Protocal.Tcp)
        {
            if (!Connected)
                return;

            try
            {
                if (type == Protocal.Tcp || !UdpEnabled)
                {
                    if (_stream != null && data != null)
                        _stream.SendMessasge(data);
                }
                else
                {
                    _server.SendUdp(data, UdpEndPoint);
                }
            }
            catch (IOException)
            {
                Close(false, "Send IOException");
            }
            catch (SocketException)
            {
                Close(false, "Send SocketException");
            }
            catch (Exception ex)
            {
                Logger.LogError("{0}: {1}\n{2}", ex.GetType(), ex.Message, ex.StackTrace);
            }
        }

        public string GetIP()
        {
            IPAddress addr = TcpEndPoint.Address;
            return addr.MapToIPv4().ToString();
        }

        public void Close(bool sendClose, string reason = "")
        {
            if (Connected)
            {
                Connected = false;
                cts.Cancel();
                if (Client != null)
                {
                    Client.Close();
                    Client = null;
                }
                if (_stream != null)
                {
                    _stream.Close();
                    _stream = null;
                }
                if (User != null)
                    User.Disconnected();
                if (CloseMessage)
                    Logger.Log("{0}: closed {1}", SessionToken, reason != "" ? "- " + reason : "");
                _server.RemoveUdpID(UdpID);
            }
        }

        private void ProcessReceiveBuffer(byte[] buffer, Protocal type)
        {
            timeOutWatch.Reset();
            timeOutWatch.Start();

            if (buffer.Length > 0)
            {
                byte command = buffer[0];
                buffer = BufferUtils.RemoveFront(BufferUtils.Remove.CMD, buffer);
                Data data = new Data(type, command, buffer);
                _server.Process(this, data);
            }
            else
                Logger.Log("{1}: Received empty buffer!", type.ToString());
        }

        private byte[] AddLength(byte[] data)
        {
            byte[] lengthBuff = BitConverter.GetBytes((UInt16)(data.Length + 2));
            return BufferUtils.Add(lengthBuff, data);
        }
    }
}