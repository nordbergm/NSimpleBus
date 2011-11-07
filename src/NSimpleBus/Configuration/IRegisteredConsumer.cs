using System;

namespace NSimpleBus.Configuration
{
    public interface IRegisteredConsumer
    {
        Type MessageType { get; }
        Type ConsumerType { get; }
        string Queue { get; }
        bool AutoDeleteQueue { get; }
        void Invoke(object message);
        Acceptance Accept(object message);
    }
}
