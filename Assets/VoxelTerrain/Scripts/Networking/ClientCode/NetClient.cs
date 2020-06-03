using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Reflection;

public class NetClient
{
    public delegate void CMD(Data data);

    public enum HostType
    {
        local,
        remote
    }

    public HostType Host_Type { get; private set; }
    public string Host { get; private set; }
    public int TcpPort = 11010;
    public int UdpPort = 11011;
    public int UdpLocalPort = 0;
    public float pingTime = 60;
    public bool udpStarted = false;
    public bool udpConnected = false;
    public int UdpID { get; set; }
    public bool IsServer { get; set; }
    public bool Run { get; private set; }
    public bool Connected { get; private set; }
    public int BytesSent_PS { get; private set; }
    public int PacketsSent_PS { get; private set; }
    public int BytesReceived_PS { get; private set; }
    public int PacketsReceived_PS { get; private set; }

    public event Action OnConnectSuccess;
    public event Action OnConnectFailed;
    public event Action OnUdpEnabled;
    public event Action<string> OnDisconnected;
    public event Action OnServerStoped;

    private TcpClient tcpClient;
    private UdpClient udpClient;
    private Socket tcpSocket;

    private IPEndPoint endPoint;

    public string username;
    public string password;
    public string sessionKey;
    public string salt;
    public byte[] ServerExponent;
    public byte[] ServerPublicKey;
    private int _receivedBytes;
    private int _received;
    private int _sentBytes;
    private int _sent;
    private Thread mainThread;

    private bool tcpPingEnabled;
    private bool udpPingEnabled;

    private Stopwatch watch;
    private Stopwatch timeOutWatch;
    private Stopwatch pingWatch;

    private ConcurrentDictionary<byte, CMD> commands;

    public NetClient(HostType connectionType, string remoteHost)
    {
        commands = new ConcurrentDictionary<byte, CMD>();
        watch = new Stopwatch();
        timeOutWatch = new Stopwatch();
        pingWatch = new Stopwatch();
        AddCommands(this);
        mainThread = Thread.CurrentThread;
        if (connectionType == HostType.local)
        {
            Host = "127.0.0.1";
        }
        else
        {
            Host = remoteHost;
        }

        pingWatch.Start();
    }

    public void Start()
    {
        ListenLoop(TcpConnect);
    }

    public void Update()
    {
        if (!Run)
            return;

        if (pingWatch.ElapsedMilliseconds > 1000)
        {
            if (tcpPingEnabled)
                TcpPing();

            if (udpPingEnabled)
                UdpPing();

            pingWatch.Restart();
        }
    }

    public void TcpConnect()
    {
        Send(0xFF, new byte[] { 0x01 }); // send connect message.
    }

    public void UdpConnect()
    {
        udpClient = new UdpClient();
        udpClient.Connect(Host, UdpPort);
        endPoint = udpClient.Client.RemoteEndPoint as IPEndPoint;
        UdpLocalPort = ((IPEndPoint)udpClient.Client.LocalEndPoint).Port;
        SafeDebug.Log(string.Format("UDP client starting: {0}, {1}, {2}", UdpLocalPort, endPoint.Port, endPoint.Address.ToString()));
        udpStarted = true;
        BegineReceiveUDP();
        Send(0xff, BufferUtils.AddFirst(0x02, BitConverter.GetBytes(UdpLocalPort)), Protocal.Udp);
        StartUdpPing();
    }

    public void StartTcpPing()
    {
        tcpPingEnabled = true;
    }

    public void StartUdpPing()
    {
        udpPingEnabled = true;
    }

    public void AddCommands(object target)
    {
        MethodInfo[] methods = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        for (int i = 0; i < methods.Length; i++)
        {
            try
            {
                ClientCommand cmdAttribute = (ClientCommand)Attribute.GetCustomAttribute(methods[i], typeof(ClientCommand));
                if (cmdAttribute != null)
                {
                    CMD function = null;
                    if (methods[i].IsStatic)
                        function = (CMD)Delegate.CreateDelegate(typeof(CMD), methods[i], true);
                    else
                        function = (CMD)Delegate.CreateDelegate(typeof(CMD), target, methods[i], true);
                    if (function != null)
                    {
                        commands[cmdAttribute.byteCommand] = function;
                    }
                    else
                    {
                        SafeDebug.LogError("Failed to add main server network function: " + methods[i].DeclaringType.Name + "." + methods[i].Name);
                    }
                }
            }
            catch (Exception e)
            {
                if (methods[i] != null)
                {
                    SafeDebug.LogError("Error adding main server network function: " + methods[i].DeclaringType.Name + "." + methods[i].Name);
                    SafeDebug.LogError(string.Format("{0}: {1}\n{2}", e.GetType(), e.Message, e.StackTrace));
                }
            }
        }
    }

    public void AddCommand(byte cmd, CMD callback)
    {
        if (!CommandExists(cmd))
        {
            commands.TryAdd(cmd, callback/*new CommandInfo(cmd, callback, async)*/);
        }
    }

    public void RemoveCommand(byte cmd)
    {
        CMD outVal;
        if (CommandExists(cmd))
            commands.TryRemove(cmd, out outVal);
    }

    public bool CommandExists(byte Cmd)
    {
        return commands.ContainsKey(Cmd);
    }

    public bool IsConnected()
    {
        if (tcpSocket == null || tcpClient == null)
            return false;
        return !(tcpSocket.Poll(1, SelectMode.SelectRead) && tcpClient.Available == 0);
    }

    public void Send(byte command, Protocal type = Protocal.Tcp)
    {
        Send(command, new byte[1], type);
    }

    public void Send(byte command, string data, Protocal type = Protocal.Tcp)
    {
        Send(InsertCommand(command, data), type);
    }

    public void Send(byte command, byte[] data, Protocal type = Protocal.Tcp)
    {
        Send(InsertCommand(command, data), type);
    }

    private void Send(byte[] data, Protocal type = Protocal.Tcp)
    {
        if ((data.Length + 2) >= 65536)
        {
            SafeDebug.LogError(string.Format("Send data length exceeds 65,536: {0} - {1}", data.Length + 2, BitConverter.ToString(data, 0, 4)));
            return;
        }

        if (Thread.CurrentThread == mainThread)
        {
            Loom.QueueAsyncTask("Net", () => {
                DoSend(data, type);
            });
        }
        else
        {
            DoSend(data, type);
        }
    }

    public void Close(string reason = "")
    {
        Connected = false;
        //CancelInvoke();
        Run = false;
        if (tcpClient != null)
        {
            tcpClient.Close();
            tcpClient = null;
            tcpSocket = null;
        }
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }
        if (OnDisconnected != null)
            OnDisconnected(reason);
    }

    private void BegineReceiveUDP()
    {
        udpClient.BeginReceive(new AsyncCallback(UdpReadCallback), null);
    }

    private void UdpReadCallback(IAsyncResult ar)
    {
        byte[] receivedBytes = udpClient.EndReceive(ar, ref endPoint);
        if (receivedBytes.Length > 0)
        {
            Process(receivedBytes, Protocal.Udp);
        }
        BegineReceiveUDP();
    }

    private void UdpPing()
    {
        Ping(Protocal.Udp);
    }

    private void TcpPing()
    {
        Ping(Protocal.Tcp);
    }

    private void Ping(Protocal type)
    {
        if (type == Protocal.Udp && !udpStarted)
            return;
        // sending "true" to tell server to ping back.
        Send(255, BufferUtils.AddFirst(0x03, BitConverter.GetBytes(true)), type);
    }

    private void ListenLoop(Action finished)
    {
        Loom.QueueAsyncTask("Net_loop", () => {
            watch.Start();
            tcpClient = new TcpClient();
            tcpClient.Connect(Host, TcpPort);
            tcpSocket = tcpClient.Client;
            Run = true;
            ManualResetEvent reset = new ManualResetEvent(false);
            timeOutWatch.Start();
            reset.WaitOne(10);
            finished();
            while (Run)
            {
                if (watch.Elapsed.Seconds >= 1)
                {
                    //BytesReceived_PS = _receivedBytes;
                    //PacketsReceived_PS = _received;
                    //BytesSent_PS = _sentBytes;
                    //PacketsSent_PS = _sent;

                    //_receivedBytes = 0;
                    //_received = 0;
                    //_sentBytes = 0;
                    //_sent = 0;

                    watch.Reset();
                    watch.Start();
                }

                if (timeOutWatch.ElapsedMilliseconds >= 10000)
                {
                    SafeDebug.Log("Connection timed out!");
                    Loom.QueueOnMainThread(() => Close("TCP Time Out."));
                    break;
                }

                if (tcpSocket.Available >= 1)
                {
                    byte[] lengthBuff = new byte[2];
                    tcpSocket.Receive(lengthBuff, 0, 2, SocketFlags.None);
                    int bufferLength = BitConverter.ToUInt16(lengthBuff, 0) - 2;
                    byte[] dataBuff = new byte[bufferLength];
                    List<byte> listBuff = new List<byte>();
                    int bytesNeeded = bufferLength;
                    while (bytesNeeded > 0)
                    {
                        byte[] partialReceiveBuff = new byte[65536];
                        int rx = tcpSocket.Receive(partialReceiveBuff, 0, bytesNeeded, SocketFlags.None);
                        byte[] partialBuff = new byte[rx];
                        Array.Copy(partialReceiveBuff, 0, partialBuff, 0, rx);
                        listBuff.AddRange(partialBuff);
                        bytesNeeded -= rx;
                    }
                    dataBuff = listBuff.ToArray();
                    timeOutWatch.Reset();
                    timeOutWatch.Start();
                    Connected = true;
                    Process(dataBuff, Protocal.Tcp);
                }
                reset.WaitOne(1);
            }
        });
    }

    private void Process(byte[] data, Protocal type)
    {
        byte command = data[0];
        byte[] dst = BufferUtils.RemoveFront(BufferUtils.Remove.CMD, data);
        if (CommandExists(command))
        {
            Loom.QueueOnMainThread(() => commands[command](new Data(type, command, dst)));
            /*if (Commands[command].Async)
                Commands[command].Cmd(new Data(type, command, dst));
            else
            {
                TaskQueue.QueueMain(() => Commands[command].Cmd(new Data(type, command, dst)));
            }*/
        }
        //Traffic traffic = new Traffic((OpCodes)command, dst);
        //ProcessData(traffic, type);
    }

    private void DoSend(byte[] data, Protocal type = Protocal.Tcp)
    {
        //SafeDebug.Log("Sending some shit 1");
        if (type == Protocal.Tcp || !udpStarted)
        {
            //SafeDebug.Log("Sending some shit 2");
            if (tcpClient != null && tcpSocket != null)
            {
                //if ((ServerCMD)data[0] != ServerCMD.Ping)
                //    SafeDebug.Log("SENDING: " + (ServerCMD)data[0]);
                data = BufferUtils.AddLength(data);
                //SafeDebug.Log("Sending some shit 3");
                tcpSocket.Send(data);
            }
            else
            {
                SafeDebug.LogError("Client TCP socket null!");
            }
        }
        else
        {
            if (udpClient != null)
            {
                byte[] udpIdBuff = BitConverter.GetBytes((UInt16)UdpID);
                byte[] buffer = BufferUtils.Add(udpIdBuff, data);
                udpClient.Send(buffer, buffer.Length);
            }
            else
                DoSend(data);
        }
    }

    private byte[] InsertCommand(byte command, string data)
    {
        return InsertCommand(command, Encoding.UTF8.GetBytes(data));
    }

    private byte[] InsertCommand(byte command, byte[] data)
    {
        return BufferUtils.AddFirst(command, data);
    }

    [ClientCommand(0xff)]
    private void SystemCmds(Data data)
    {
        byte cmd = data.Buffer[0];
        data.Buffer = BufferUtils.RemoveFront(BufferUtils.Remove.CMD, data.Buffer);
        switch (cmd)
        {
            case 0x01: // connected.
                if (data.Buffer[0] == 0x01)
                {
                    UdpID = BitConverter.ToUInt16(data.Buffer, 1);
                    StartTcpPing();
                    if (OnConnectSuccess != null)
                        OnConnectSuccess();
                }
                else
                {
                    if (OnConnectFailed != null)
                        OnConnectFailed();
                    Close();
                }
                break;

            case 0x02: // udp enabled
                udpConnected = true;
                if (OnUdpEnabled != null)
                    OnUdpEnabled();
                break;

            case 0x03: // ping.
                bool pingBack = BitConverter.ToBoolean(data.Buffer, 0);
                if (pingBack)
                {
                    Send(0xff, BufferUtils.AddFirst(0x03, BitConverter.GetBytes(false)), data.Type);
                }
                break;

            case 0x04: // server closed.
                if (OnServerStoped != null)
                    OnServerStoped();
                Close();
                break;

            default:
                SafeDebug.LogError("Received invalid system command: " + cmd);
                break;
        }
    }
}
