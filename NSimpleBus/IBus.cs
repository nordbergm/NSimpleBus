using System;
using NSimpleBus.Configuration;

namespace NSimpleBus
{
    public interface IBus : IDisposable
    {
        IBrokerConfiguration Configuration { get; }
        void Publish<T>(T message) where T : class;
    }
}