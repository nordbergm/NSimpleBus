using System.Collections.Generic;

namespace NSimpleBus.Transports
{
    public interface IMessageEnvelope<out T> where T : class
    {
        T Message { get; }
        string UserName { get; }
        IDictionary<string, string> Headers { get; }
    }
}