using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;

namespace ProtoTest.Common
{
    public enum ServerClientStatus
    {
        Idle,
        ReadData
    }

    public class ServerClient<TMessage> : IDisposable where TMessage : IMessage, IMessage<TMessage>, new()
    {
        public TcpClient Client { get; }

        public void Dispose()
        {
            Client?.Dispose();
        }

        private Stream Stream => Client.GetStream();

        public ServerClientStatus Status { get; private set; } = ServerClientStatus.Idle;

        private int DataLength { get; set; } = 0;
        private int DataPosition { get; set; } = 0;
        private byte[] Data { get; set; }
        private MessageParser<TMessage> Parser { get; }
        private ConcurrentQueue<TMessage> SendQueue { get; } = new ConcurrentQueue<TMessage>();

        public event EventHandler<MessageEvent<TMessage>> MessageReceived;
        public event EventHandler RecieveError;

        public ServerClient(TcpClient client)
        {
            Client = client;
            Parser = new MessageParser<TMessage>(() => new TMessage());
        }

        private void Reset()
        {
            Status = ServerClientStatus.Idle;
            DataLength = 0;
            DataPosition = 0;
            Data = null;
        }

        public async Task Poll()
        {
            if (!Client.Connected || Client.Available < 0)
            {
                return;
            }

            switch (Status)
            {
                case ServerClientStatus.Idle:
                    if (Client.Available >= sizeof(int))
                    {
                        var buf = new byte[sizeof(int)];
                        await Stream.ReadAsync(buf, 0, buf.Length);
                        DataLength = BitConverter.ToInt32(buf, 0);
                        Data = new byte[DataLength];
                        DataPosition = 0;
                        Status = ServerClientStatus.ReadData;
                    }
                    break;
                case ServerClientStatus.ReadData:

                    DataPosition += await Stream.ReadAsync(Data, DataPosition, DataLength - DataPosition);
                   
                    if (DataPosition == DataLength)
                    {
                        try
                        {
                            var msg = Parser.ParseFrom(Data);
                            await Task.Run(() => MessageReceived?.Invoke(this, new MessageEvent<TMessage>(msg)));
                        }
                        catch (InvalidProtocolBufferException)
                        {
                            await Task.Run(() => RecieveError?.Invoke(this, EventArgs.Empty));
                        }
                        finally
                        {
                            Reset();
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (SendQueue.TryDequeue(out var sendMessage))
            {
                await Send(sendMessage);
            }
        }

        public void EnqueueMessage(TMessage msg)
        {
            SendQueue.Enqueue(msg);
        }

        private async Task Send(TMessage msg)
        {
            var length = msg.CalculateSize();
            var data = new byte[length + sizeof(int)];
            BitConverter.GetBytes(length).CopyTo(data, 0);
            using (var ms = new MemoryStream(data, true))
            {
                ms.Seek(sizeof(int), SeekOrigin.Begin);
                msg.WriteTo(ms);
            }

            await Stream.WriteAsync(data, 0, data.Length);
        }
    }
}
