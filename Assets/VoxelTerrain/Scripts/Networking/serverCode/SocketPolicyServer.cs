using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace UnityGameServer
{
    public class SocketPolicyServer
    {
        const string PolicyFileRequest = "<policy-file-request/>";
        static byte[] request = Encoding.UTF8.GetBytes(PolicyFileRequest);
        private byte[] policy;

        private Socket listen_socket;
        private static SocketPolicyServer _server;

        private AsyncCallback accept_cb;

        private bool _run;
        private Thread _thread;

        class Request
        {
            public Request(Socket s)
            {
                Socket = s;
                // the only answer to a single request (so it's always the same length)
                Buffer = new byte[request.Length];
                Length = 0;
            }

            public Socket Socket { get; private set; }
            public byte[] Buffer { get; set; }
            public int Length { get; set; }
        }

        public SocketPolicyServer(string xml)
        {
            // transform the policy to a byte array (a single time)
            policy = Encoding.UTF8.GetBytes(xml);
        }

        public int Start()
        {
            try
            {
                _run = true;
                listen_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listen_socket.Bind(new IPEndPoint(IPAddress.Any, 843));
                listen_socket.Listen(500);
                listen_socket.Blocking = false;
                _thread = new Thread(RunServer);
                _thread.Start();
            }
            catch (SocketException se)
            {
                // Most common mistake: port 843 is not user accessible on unix-like operating systems
                if (se.SocketErrorCode == SocketError.AccessDenied)
                {
                    Logger.LogError("NOTE: must be run as root since the server listen to port 843");
                    return 5;
                }
                else
                {
                    Logger.LogError("{0}: {1}\n{2}", se.GetType(), se.Message, se.StackTrace);
                    return 6;
                }
            }
            catch (ThreadAbortException) { }
            return 0;
        }

        void RunServer()
        {
            try
            {
                accept_cb = new AsyncCallback(OnAccept);
                BeginAccept();

                while (_run) // Just sleep until we're aborted.
                    Thread.Sleep(1000);
            }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                Logger.Log("{0}: {1}\n{2}", ex.GetType(), ex.Message, ex.Message);
            }
            finally
            {
                Logger.Log("socket policy server Closed.");
            }
        }

        void BeginAccept()
        {
            try
            {
                if (!_run)
                    return;
                listen_socket.BeginAccept(accept_cb, null);
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
            catch (ThreadAbortException) { }
        }

        void OnAccept(IAsyncResult ar)
        {
            try
            {

                //Logger.Log("incoming connection");
                Socket accepted = null;
                try
                {
                    accepted = listen_socket.EndAccept(ar);
                }
                catch (SocketException)
                {
                    accepted = null;
                }
                catch (ThreadAbortException)
                {
                    accepted = null;
                }
                catch (ObjectDisposedException)
                {
                    accepted = null;
                }
                finally
                {
                    BeginAccept();
                }


                if (accepted == null || !_run)
                    return;


                accepted.Blocking = true;

                Request request = new Request(accepted);
                accepted.BeginReceive(request.Buffer, 0, request.Length, SocketFlags.None, new AsyncCallback(OnReceive), request);
            }
            catch (ThreadAbortException)
            {
                return;
            }
        }

        void OnReceive(IAsyncResult ar)
        {
            if (!_run)
                return;

            Request r = (ar.AsyncState as Request);
            Socket socket = r.Socket;
            try
            {
                r.Length += socket.EndReceive(ar);

                // compare incoming data with expected request
                for (int i = 0; i < r.Length; i++)
                {
                    if (r.Buffer[i] != request[i])
                    {
                        // invalid request, close socket
                        socket.Close();
                        return;
                    }
                }

                if (r.Length == request.Length)
                {
                    Logger.Log("got policy request, sending response");
                    // request complete, send policy
                    socket.BeginSend(policy, 0, policy.Length, SocketFlags.None, new AsyncCallback(OnSend), socket);
                }
                else
                {
                    // continue reading from socket
                    socket.BeginReceive(r.Buffer, r.Length, request.Length - r.Length, SocketFlags.None,
                        new AsyncCallback(OnReceive), ar.AsyncState);
                }
            }
            catch
            {
                // if anything goes wrong we stop our connection by closing the socket
                socket.Close();
            }
        }

        void OnSend(IAsyncResult ar)
        {
            if (!_run)
                return;

            Socket socket = (ar.AsyncState as Socket);
            try
            {
                socket.EndSend(ar);
            }
            catch
            {
                // whatever happens we close the socket
            }
            finally
            {
                socket.Close();
            }
        }

        public static void Stop()
        {
            if (_server != null)
            {
                _server.listen_socket.Close();
                _server._run = false;
                _server = null;
            }
        }

        const string AllPolicy =

    @"<?xml version='1.0'?>
<cross-domain-policy>
        <allow-access-from domain=""*"" to-ports=""*"" />
</cross-domain-policy>";

        const string LocalPolicy =

    @"<?xml version='1.0'?>
<cross-domain-policy>
	<allow-access-from domain=""*"" to-ports=""11010"" />
</cross-domain-policy>";


        public static void LoadAll()
        {
            string policy = AllPolicy;
            _server = new SocketPolicyServer(policy);
            int result = _server.Start();
        }

        public static void LoadLocal()
        {
            string policy = LocalPolicy;
            _server = new SocketPolicyServer(policy);
            int result = _server.Start();
        }

        public static void LoadPort(int port)
        {
            string policy = string.Format(
            @"<?xml version='1.0'?>
              <cross-domain-policy>
	              <allow-access-from domain=""*"" to-ports=""{0}"" />
              </cross-domain-policy>", port);
            _server = new SocketPolicyServer(policy);
            int result = _server.Start();
        }

        public static void LoadPort(int startPort, int endPort)
        {
            string policy = string.Format(
            @"<?xml version='1.0'?>
              <cross-domain-policy>
	              <allow-access-from domain=""*"" to-ports=""{0}-{1}"" />
              </cross-domain-policy>", startPort, endPort);
            _server = new SocketPolicyServer(policy);
            int result = _server.Start();
        }

        public static void LoadFile(string filename)
        {
            string policy = string.Empty;
            if (!File.Exists(filename))
            {
                Logger.LogError("Could not find policy file '{0}'.", filename);
                ;
                return;
            }
            using (StreamReader sr = new StreamReader(filename))
            {
                policy = sr.ReadToEnd();
            }
            _server = new SocketPolicyServer(policy);
            int result = _server.Start();
        }

    }
}
