using System;
using System.Net;
using System.Net.Sockets;

namespace da_pak
{
    class Pak
    {
        static void Initialize() {
            TcpListener server = null;
            try
            {
                Int16 port = 1300;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");

                server = new TcpListener(localAddr, port);
                server.Start();

                Byte[] bytes = new Byte[256];
                String data = null;

                while(!false)
                {
                    System.Console.WriteLine("Waiting for a connection... ");

                    /* Perf. blocking call to accpt requests */
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    data = null;

                    NetworkStream stream = client.GetStream();
                    client.Close();
                }
            }
            catch(SocketException e)        { Console.WriteLine("\nSocketException: {0}", e);                       }
            catch(System.IO.IOException e)  { Console.WriteLine("\nClient'z connection was interrupted!\n{0}", e);  }
            finally
            {
                server.Stop();
            }

            System.Console.WriteLine("\nHit Enter to continue...");
            Console.Read();
        }

        static void Main(string[] args)
        {
            Initialize();
        }
    }
}
