using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

namespace da_pak
{
    class Pak
    {
        private static byte[] _buffer = new byte[2048];
        private static List<Socket> _clientSockets = new List<Socket>();
        private static Socket _serverSocket = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        static void Main(string[] args)
        {
            Console.Title = "Pak";

            SetupServer();
            SendLoop();

            Console.WriteLine("Exiting...");
            Console.ReadLine();
        }

        private static void SetupServer()
        {
            Console.WriteLine("Setting up server...");
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, 1337));
            
            _serverSocket.Listen(5);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private static void AcceptCallback(IAsyncResult AR)
        {
            Socket socket = _serverSocket.EndAccept(AR);

            _clientSockets.Add(socket);
            Console.WriteLine("Client connected...");
            socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private static void ReceiveCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            
            try
            {
                int received = socket.EndReceive(AR);

                byte[] dataBuf = new byte[received];
                Array.Copy(_buffer, dataBuf, received);

                string text = Encoding.ASCII.GetString(dataBuf);
                // Console.WriteLine("Text received: {0}", text); 

                // HANDLE REQUEST

                string response = string.Empty;
                if (text.ToLower() != "get time")
                {
                    response = "Invalid Request";
                }
                else 
                {
                    response = DateTime.Now.ToLongTimeString();
                }

                byte[] data = Encoding.ASCII.GetBytes(response);
                socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);

                socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            }
            catch (SocketException) { Console.WriteLine("Client unexpectedly disconnected..."); }
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
                string input = Console.ReadLine().Trim();

                if (input == "break" || input == "exit")
                    break;

                byte[] data = Encoding.ASCII.GetBytes(input);

                foreach(var sok in _clientSockets)
                {
                    sok.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), sok);
                    // sok.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), sok);
                }
            }
        }
    }
}
