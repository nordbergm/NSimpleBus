namespace NSimpleBus.Transports
{
    public interface IMessageEnvelope<out T> where T : class
    {
        T Message { get; }
        string MessageType { get; }
        string UserName { get; }
    }
}