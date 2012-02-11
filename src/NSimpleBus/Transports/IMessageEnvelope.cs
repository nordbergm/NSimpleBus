using System.Collections.Specialized;

namespace NSimpleBus.Transports
{
    public interface IMessageEnvelope<out T> where T : class
    {
        T Message { get; }
        string UserName { get; }
        NameValueCollection Headers { get; }
    }
}