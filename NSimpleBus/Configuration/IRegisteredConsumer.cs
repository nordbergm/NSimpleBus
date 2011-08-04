using System;

namespace NSimpleBus.Configuration
{
    public interface IRegisteredConsumer
    {
        IConsumer Consumer { get; }
        Type MessageType { get; }
        string Queue { get; }
        void Invoke(object message);
    }
}
