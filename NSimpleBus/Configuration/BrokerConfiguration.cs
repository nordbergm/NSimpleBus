using System;
using System.Collections.Generic;
using System.Reflection;
using NSimpleBus.Serialization;
using NSimpleBus.Transports;

namespace NSimpleBus.Configuration
{
    public class BrokerConfiguration : IBrokerConfiguration
    {
        public BrokerConfiguration()
        {
            RegisteredConsumers = new Dictionary<Type, IRegisteredConsumer>();
            AutoConfigure = AutoConfigureMode.None;
        }

        public string UserName { get; set; }
        public string Password { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; }
        public ISerializer Serializer { get; set; }
        public string Exchange { get; set; }
        public string VirtualHost { get; set; }
        public AutoConfigureMode AutoConfigure { get; set; }
        public IDictionary<Type, IRegisteredConsumer> RegisteredConsumers { get; set; }
        public IBrokerConnectionFactory ConnectionFactory { get; set; }

        public void RegisterConsumer(IConsumer consumer)
        {
            Type[] ifaces = consumer.GetType().GetInterfaces();

            foreach (Type iface in ifaces)
            {
                if (!typeof (IConsumer).IsAssignableFrom(iface))
                {
                    continue;
                }

                var gargs = iface.GetGenericArguments();


                if (gargs.Length > 0)
                {
                    Type messageType = iface.GetGenericArguments()[0];
                    RegisteredConsumers.Add(messageType, new RegisteredConsumer(messageType, consumer));
                }
            }

            if (RegisteredConsumers.Count == 0)
            {
                throw new ArgumentException("Unable to find any messages to consume.", "consumer");
            }
        }

        public class RegisteredConsumer : IRegisteredConsumer
        {
            public RegisteredConsumer(Type messageType, IConsumer consumer)
            {
                MessageType = messageType;
                Queue = messageType.FullName;
                Consumer = consumer;
                ConsumeMethod = consumer.GetType().GetMethod("Consume", new[] { messageType });
            }

            public Type MessageType { get; private set; }
            public IConsumer Consumer { get; private set; }
            public MethodInfo ConsumeMethod { get; private set; }
            public string Queue { get; private set; }

            public void Invoke(object message)
            {
                ConsumeMethod.Invoke(Consumer, new [] { message });
            }
        }
    }
}
