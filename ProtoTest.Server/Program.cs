using System;
using System.Threading;

namespace ProtoTest.Server
{
    class Program
    {
        private static volatile bool _running = true;

        static void Main(string[] args)
        {
            var serverThread = new Thread(Server);
            serverThread.Start();

            Console.Write("Server Running. Press any key to quit.");
            Console.ReadKey(true);
            _running = false;

            serverThread.Join();
        }

        static void Server()
        {
            var server = new Common.Server(30123);
            server.Start();
            while (_running)
            {
                server.Poll().Wait();
                Thread.Sleep(10);
            }
            server.Stop();
        }
    }
}
