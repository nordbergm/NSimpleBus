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
        CreatePrincipalDelegate CreatePrincipal { get; set; }
        ISerializer Serializer { get; set; }
        IDictionary<Type, IList<IRegisteredConsumer>> RegisteredConsumers { get; set; }
        IBrokerConnectionFactory ConnectionFactory { get; set; }
        void RegisterConsumer(Func<IConsumer> consumer);
        void RegisterSubscriber(Func<ISubscriber> consumer);
        void RegisterConsumers(Assembly assembly, string nameSpace = null, Func<Type, IConsumer> resolver = null);
        void RegisterConsumers(Assembly[] assemblies, string[] nameSpaces = null, Func<Type, IConsumer> resolver = null);
        void RegisterSubscribers(Assembly assembly, string nameSpace = null, Func<Type, ISubscriber> resolver = null);
        void RegisterSubscribers(Assembly[] assemblies, string[] nameSpaces = null, Func<Type, ISubscriber> resolver = null);
    }
}