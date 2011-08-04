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

        public void RegisterSubscriber(IConsumer consumer)
        {
            RegisterConsumer(consumer, (t, c) => new RegisteredPubSubConsumer(t, c));
        }

        public void RegisterConsumer(IConsumer consumer)
        {
            RegisterConsumer(consumer, (t, c) => new RegisteredConsumer(t, c));
        }

        private void RegisterConsumer(IConsumer consumer, Func<Type, IConsumer, IRegisteredConsumer> newConsumer)
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
                    RegisteredConsumers.Add(messageType, newConsumer(messageType, consumer));
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

            public Type MessageType { get; protected set; }
            public IConsumer Consumer { get; protected set; }
            public MethodInfo ConsumeMethod { get; protected set; }
            public string Queue { get; protected set; }

            public void Invoke(object message)
            {
                ConsumeMethod.Invoke(Consumer, new [] { message });
            }
        }

        public class RegisteredPubSubConsumer : RegisteredConsumer
        {
            public RegisteredPubSubConsumer(Type messageType, IConsumer consumer) : base(messageType, consumer)
            {
                this.Queue = string.Format("{0}.{1}", this.Queue, Guid.NewGuid().ToString("n"));
            }
        }
    }
}
