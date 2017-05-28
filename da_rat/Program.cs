using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace da_rat
{
    class Rat
    {
        private static ManualResetEvent _connectDone = new ManualResetEvent(false);
        private static Socket _clientSocket =
            new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
        static void Main(string[] args)
        {
            Console.Title = "Rat";

            // LoopConnect();
            ConnectAsync();
            SendLoop();

            Console.WriteLine("Exiting...");
            Console.ReadLine();
        }

        private static void ConnectAsync(string home = "127.0.0.1", int port = 1337)
        {
            _clientSocket.BeginConnect(IPAddress.Parse(home), port
                , new AsyncCallback(ConnectCallback), _clientSocket);

            _connectDone.WaitOne();
        }

        private static void ConnectCallback(IAsyncResult AR)
        {
            try
            {
                /* Retreive the socket from the state obj & complete conn. */
                Socket client = (Socket)AR.AsyncState;
                client.EndConnect(AR);

                Console.WriteLine("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());

                _connectDone.Set();
            } catch (Exception e) { Console.WriteLine(e.ToString()); }
        }

        /* TODO: RECEIVE CALLBACK
        ***********************************************************/

/* OldConn
        private static void LoopConnect()
        {
            int attempts = 0;

            while(!_clientSocket.Connected)
            {
                try
                {
                    attempts += 1;
                    _clientSocket.Connect(IPAddress.Loopback, 1337); // Change loopback with main IP
                }
                catch (SocketException)
                {
                    Console.Clear();
                    Console.WriteLine("Connection attempts: {0}", attempts.ToString());
                }

                Console.Clear();
                Console.WriteLine("Connected...");
            }
        }
*/

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

                byte[] recBuf = new byte[2048];
                int rec = _clientSocket.Receive(recBuf);

                byte[] data = new byte[rec];
                Array.Copy(recBuf, data, rec);

                Console.WriteLine("Received: {0}", Encoding.ASCII.GetString(data));
            }
        }
    }
}
