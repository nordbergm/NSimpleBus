using NSimpleBus.Serialization;
using RabbitMQ.Client;

namespace NSimpleBus.Transports.RabbitMQ.Serialization
{
    public interface IMessageSerializer
    {
        ISerializer Serializer { get; }
        IMessageEnvelope<object> DeserializeMessage(QueueActivityConsumer.DeliverEventArgs args);
        void SerializeMessage(IMessageEnvelope<object> message, IModel model, out IBasicProperties headers, out byte[] body, out string routingKey);
    }
}