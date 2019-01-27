using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Google.Protobuf;
using ProtoTest.Common;
using ProtoTest.Common.Data;

namespace ProtoTest
{
    class Program
    {
        private static volatile bool _running = true;

        static void Main(string[] args)
        {
            var tcpClient = new TcpClient();
            
            var client = new ServerClient<ChatMessage>(tcpClient);
            client.MessageReceived += (s, e) => Console.WriteLine($"{e.Message.From}: {e.Message.Msg}");
            
            var id = 0;

            Console.Write("Who are you? ");
            var name = Console.ReadLine();
            client.EnqueueMessage(new ChatMessage {Id = id++, From = name, Msg = "Logged in"});;
            
            var clientThread = new Thread(() => Client(client));
            clientThread.Start();

            tcpClient.Connect(IPAddress.Loopback, 30123);

            string line;
            while(!string.IsNullOrEmpty(line = Console.ReadLine()))
            {
                client.EnqueueMessage(new ChatMessage {Id = id++, From = name, Msg = line});
            }

            _running = false;
            clientThread.Join();
        }

        static void Client(ServerClient<ChatMessage> client)
        {
            while (_running)
            {
                client.Poll().Wait();
                Thread.Sleep(10);
            }
        }
    }
}
