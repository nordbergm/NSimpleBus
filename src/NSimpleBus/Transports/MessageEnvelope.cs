namespace NSimpleBus.Transports
{
    public class MessageEnvelope<T> : IMessageEnvelope<T> where T : class
    {
        public MessageEnvelope(T message)
        {
            Message = message;
            MessageType = message.GetType().AssemblyQualifiedName;
        }

        public T Message { get; private set; }
        public string MessageType { get; private set; }
    }
}
