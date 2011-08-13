using System;
using System.Collections.Generic;
using System.Reflection;
using NSimpleBus.Serialization;
using NSimpleBus.Transports;

namespace NSimpleBus.Configuration
{
    public interface IBrokerConfiguration
    {
        string UserName { get; set; }
        string Password { get; set; }
        string HostName { get; set; }
        int Port { get; set; }
        string Exchange { get; set; }
        string VirtualHost { get; set; }
        AutoConfigureMode AutoConfigure { get; set; }
        ISerializer Serializer { get; set; }
        IDictionary<Type, IRegisteredConsumer> RegisteredConsumers { get; set; }
        IBrokerConnectionFactory ConnectionFactory { get; set; }
        void RegisterConsumer(Func<IConsumer> consumer);
        void RegisterSubscriber(Func<ISubscriber> consumer);
        void RegisterConsumers(Assembly assembly);
        void RegisterSubscribers(Assembly assembly);
    }
}