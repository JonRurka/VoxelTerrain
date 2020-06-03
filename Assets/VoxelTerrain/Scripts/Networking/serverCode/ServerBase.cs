using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using UnityGameServer.Networking;

namespace UnityGameServer
{
    public class ServerBase : IDisposable
    {
        public static ServerBase BaseInstance;
        /// <summary>
        /// Director separator character.
        /// </summary>
        public static char sepChar { get { return Path.DirectorySeparatorChar; } }
        /// <summary>
        /// returns true if the server is being ran on mono (non-windows usually).
        /// </summary>
        public static bool isMono { get { return (Type.GetType("Mono.Runtime") != null); } }

        /// <summary>
        /// Command line args.
        /// </summary>
        public string[] CmdArgs { get; private set; }
        /// <summary>
        /// Directory applications is located in.
        /// </summary>
        public string AppDirectory { get; private set; }
        /// <summary>
        /// Specifies if the server's primary loop is currently running.
        /// </summary>
        public bool Running { get; private set; }

        /// <summary>
        /// Handle for console input.
        /// </summary>
        //public CommandExecuter CommandInput { get; protected set; }
        /// <summary>
        /// Basic settings loaded from Settings.ini.
        /// </summary>
        public AppSettings BaseSettings { get; protected set; }
        /// <summary>
        /// Handles Tcp and Udp networking.
        /// </summary>
        public AsyncServer Server { get; protected set; }

        private ManualResetEvent _resetEvent;
        private TaskQueue _taskQueue;

        /// <summary>
        /// Server base class constructor.
        /// </summary>
        /// <param name="args">command line args.</param>
        public ServerBase(string[] args)
        {
            BaseInstance = this;
            CmdArgs = args;
            _taskQueue = new TaskQueue();
            AppDirectory = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName + sepChar;
            _resetEvent = new ManualResetEvent(false);
            LoadSettings(AppDirectory + "Settings.ini");
        }

        /// <summary>
        /// Initializes the server and starts the primary application loop.
        /// Thread blocking call that doesn't return until Stop or Dispose is called.
        /// </summary>
        public void Start()
        {
            Init();
            Loop();
        }

        /// <summary>
        /// Used to load settings from Settings.ini.
        /// Called before Init.
        /// </summary>
        /// <param name="file"></param>
        public virtual void LoadSettings(string file)
        {
            BaseSettings = new AppSettings(file);
        }

        /// <summary>
        /// Initializes the various modules required by the server.
        /// Called after LoadSettings and before Update.
        /// </summary>
        public virtual void Init()
        {
            Running = true;
            //CommandInput = new CommandExecuter();
            SocketPolicyServer.LoadPort(AppSettings.UdpPort, AppSettings.UdpPort);
            Server = new AsyncServer(AppSettings.TcpPort, AppSettings.UdpPort);
            Server.AddCommands(this);
        }

        private void Loop()
        {
            while (Running)
            {
                try
                {
                    _resetEvent.WaitOne(1);
                    Update();
                }
                catch (Exception e)
                {
                    Logger.LogError("{0}: {1}\n{2}", e.GetType(), e.Message, e.StackTrace);
                }
            }
        }

        /// <summary>
        /// Called every frame by the main loop.
        /// </summary>
        public virtual void Update()
        {
            if (!Running)
                return;

            //CommandInput.Update();
            _taskQueue.Update();
            Logger.Update();
        }

        /// <summary>
        /// Called when a user connects to the server.
        /// </summary>
        /// <param name="user">socket user who connected.</param>
        public virtual void UserConnected(SocketUser user)
        {

        }

        /// <summary>
        /// Called when a connected user enabled the UDP network protocal.
        /// </summary>
        /// <param name="user">Socket user who enabled UDP.</param>
        public virtual void UserUdpEnabled(SocketUser user)
        {

        }

        /// <summary>
        /// Called when a socket user pings the server.
        /// </summary>
        /// <param name="user">Socket user who sent ping.</param>
        /// <param name="protocal">Protocal ping was sent with.</param>
        /// <param name="pingBack">Whether a response was sent back the user.</param>
        public virtual void UserPinged(SocketUser user, Protocal protocal, bool pingBack)
        {

        }

        /// <summary>
        /// Stop Server the server.
        /// </summary>
        public virtual void Stop()
        {
            Logger.Log("stopping server...");
            Running = false;
        }

        /// <summary>
        /// Dispose various modules used by the server.
        /// </summary>
        public virtual void Dispose()
        {
            Logger.Log("disposing...");
            Running = false;
            _resetEvent.Dispose();
            if (Server != null)
            {
                Server.Stop(true);
                Server = null;
            }
            /*if (CommandInput != null)
            {
                CommandInput.Close();
            }*/
            SocketPolicyServer.Stop();
            TaskQueue.Close();
            Logger.Log("disposed!");
        }

        [Command(0xff)]
        protected void SystemCmds(SocketUser user, Data data)
        {
            byte cmd = data.Buffer[0];
            data.Buffer = BufferUtils.RemoveFront(BufferUtils.Remove.CMD, data.Buffer);
            switch (cmd)
            {
                case 0x02: // udp enable
                    //Logger.Log("{0}: Udp enabled", user.SessionToken);
                    int port = BitConverter.ToInt32(data.Buffer, 0);
                    user.EnableUdp(port);
                    user.Send(0xff, new byte[] { 0x02 }, Protocal.Udp);
                    UserUdpEnabled(user);
                    break;

                case 0x03: // ping
                    bool pingBack = BitConverter.ToBoolean(data.Buffer, 0);
                    if (pingBack)
                    {
                        user.Send(0xff, BufferUtils.AddFirst(0x03, BitConverter.GetBytes(false)), data.Type);
                    }
                    UserPinged(user, data.Type, pingBack);
                    break;

                default:
                    Logger.LogError("Received invalid system command: {0}", cmd);
                    break;
            }
        }
    }
}