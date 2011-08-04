using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using NSimpleBus.Serialization;
using RabbitMQ.Client;
using RabbitMQ.Client.Content;

namespace NSimpleBus.Transports.RabbitMQ.Serialization
{
    public class MessageSerializer
    {
        private const string MessageTypeHeader = "MessageType";

        public MessageSerializer(ISerializer serializer)
        {
            Serializer = serializer;
        }

        public ISerializer Serializer { get; private set; }

        public IMessageEnvelope<object> DeserializeMessage(QueueActivityConsumer.DeliverEventArgs args)
        {
            byte[] messageTypeHeader = args.Properties.Headers[MessageTypeHeader] as byte[];

            Type messageType = null;
            if (messageTypeHeader != null && messageTypeHeader.Length > 0)
            {
                messageType = Type.GetType(Encoding.Default.GetString(messageTypeHeader));
            }

            if (messageType == null)
            {
                throw new SerializationException(string.Format("The message of type '{0}' could not be found.", messageTypeHeader));
            }

            Stream messageStream = new MemoryStream(args.Body);
            Type envelopeType = typeof(MessageEnvelope<>).GetGenericTypeDefinition().MakeGenericType(messageType);

            return (IMessageEnvelope<object>)Serializer.Deserialize(messageStream, envelopeType);
        }

        public void SerializeMessage(IMessageEnvelope<object> message, IModel model, out IBasicProperties headers, out byte[] body, out string routingKey)
        {
            Stream stream = Serializer.Serialize(message);

            var messageBuilder = new MapMessageBuilder(model);
            messageBuilder.Headers[MessageTypeHeader] = message.MessageType;
            headers = (IBasicProperties)messageBuilder.GetContentHeader();

            routingKey = message.Message.GetType().ToRoutingKey();
            
            body = new byte[stream.Length];
            stream.Read(body, 0, body.Length);
        }
    }
}
