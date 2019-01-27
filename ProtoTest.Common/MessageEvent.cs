using System;
using Google.Protobuf;

namespace ProtoTest.Common
{
    public class MessageEvent<TMessage> : EventArgs where TMessage : IMessage, IMessage<TMessage>
    {
        public TMessage Message { get; }

        public MessageEvent(TMessage message)
        {
            Message = message;
        }
    }
}