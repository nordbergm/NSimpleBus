using System;
using NSimpleBus.Configuration;

namespace NSimpleBus.Transports
{
    public interface IBrokerConnection : IDisposable
    {
        bool IsOpen { get; }
        void Consume(IRegisteredConsumer registeredConsumer);
        void Publish<T>(IMessageEnvelope<T> message) where T : class;
        void Close();
    }
}
