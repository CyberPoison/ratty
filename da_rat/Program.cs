using System;
using System.Net;
using System.Net.Sockets;

namespace da_rat
{
    class Rat
    {
        static void Connect(String server, String message)
        {
            TcpClient client = null;
            try
            {
                Int16 port = 1300;
                client = new TcpClient(server, port);

                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

                // Stream stream = client.GetStream();
                NetworkStream stream = client.GetStream();

                stream.Write(data, 0, data.Length);
                Console.WriteLine("Sent: {0}", message);

                data = new Byte[256];

                String responseData = String.Empty;

                int bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Console.WriteLine("Received: {0}", responseData);
            }
            catch (ArgumentNullException e)   { Console.WriteLine("\nArgumentNullException: {0}", e);   }
            catch (SocketException e)         { Console.WriteLine("\nServer not found! Closing... "); System.Environment.Exit(e.ErrorCode); }
            finally
            {
                client.Close();
            }

            Console.WriteLine("\n Press Enter to continue...");
            Console.Read();
        }

        static void Main(string[] args)
        {
            string mahssage = "Dickbutt";
            Connect("127.0.0.1", mahssage);
        }
    }
}
