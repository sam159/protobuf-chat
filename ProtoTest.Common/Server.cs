using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Google.Protobuf;
using ProtoTest.Common.Data;

namespace ProtoTest.Common
{
    public class Server
    {
        public int Port { get; }

        private TcpListener Listener { get; }

        private IList<ServerClient<ChatMessage>> Clients { get; } = new List<ServerClient<ChatMessage>>();

        private int NextId { get; set; } = 0;
        private object NextIdLock { get; set; } = new object();

        public Server(int port)
        {
            Port = port;

            Listener = new TcpListener(IPAddress.Any, Port);
        }

        public void Start()
        {
            Listener.Start(10);
        }

        public void Stop()
        {
            Listener.Stop();
        }

        public int GetNextId()
        {
            lock (NextIdLock)
            {
                return NextId++;
            }
        }

        public async Task Poll()
        {
            while (Listener.Pending())
            {
                var tcpClient = await Listener.AcceptTcpClientAsync();
                var client = new ServerClient<ChatMessage>(tcpClient);
                client.MessageReceived += ClientOnMessageReceived;
                Clients.Add(client);
            }

            var disconnectedClients = Clients.Where(c => !c.Client.Connected).ToArray();
            foreach (var client in disconnectedClients)
            {
                Clients.Remove(client);
                client.Dispose();
            }

            await Task.WhenAll(Clients.Select(c => c.Poll()));
        }

        private void ClientOnMessageReceived(object sender, MessageEvent<ChatMessage> e)
        {
            Console.WriteLine(e.Message);
            foreach (var client in Clients)
            {
                client.EnqueueMessage(new ChatMessage{Id = GetNextId(), From = e.Message.From, Msg = e.Message.Msg});
            }
        }
    }
}
