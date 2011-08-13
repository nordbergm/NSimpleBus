using System;

namespace NSimpleBus.Configuration
{
    public interface IRegisteredConsumer
    {
        Type MessageType { get; }
        string Queue { get; }
        void Invoke(object message);
    }
}
