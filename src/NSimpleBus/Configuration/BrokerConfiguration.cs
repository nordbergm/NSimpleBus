using System;
using System.Collections.Generic;
using System.Linq;
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

        public void RegisterConsumers(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            foreach (var type in assembly.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IConsumer))))
            {
                Type consumerType = type;
                RegisterConsumer(() => (IConsumer)Activator.CreateInstance(consumerType), (t, c) => new RegisteredConsumer(t, c));
            }
        }

        public void RegisterSubscribers(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            foreach (var type in assembly.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(ISubscriber))))
            {
                Type consumerType = type;
                RegisterConsumer(() => (ISubscriber)Activator.CreateInstance(consumerType), (t, c) => new RegisteredSubscriber(t, c));
            }
        }

        public void RegisterSubscriber(Func<ISubscriber> subscriberDelegate)
        {
            RegisterConsumer(subscriberDelegate, (t, c) => new RegisteredSubscriber(t, c));
        }

        public void RegisterConsumer(Func<IConsumer> consumerDelegate)
        {
            RegisterConsumer(consumerDelegate, (t, c) => new RegisteredConsumer(t, c));
        }

        private void RegisterConsumer<T>(Func<T> consumerDelegate, Func<Type, Func<T>, IRegisteredConsumer> newConsumer) where T : class
        {
            if (consumerDelegate == null)
            {
                throw new ArgumentNullException("consumerDelegate");
            }

            T consumer;

            try
            {
                consumer = consumerDelegate.Invoke();
            }
            catch (Exception ex)
            {
                throw new TargetInvocationException(
                    string.Format("An exception was thrown when invoking the consumer delegate '{0}.", consumerDelegate.GetType().GetGenericArguments()[0].FullName), ex);
            }

            if (consumer == null)
            {
                throw new ArgumentNullException("consumerDelegate", "The consumer delegate returned null.");
            }

            Type[] ifaces = consumer.GetType().GetInterfaces();

            foreach (Type iface in ifaces)
            {
                if (!typeof (T).IsAssignableFrom(iface))
                {
                    continue;
                }

                var gargs = iface.GetGenericArguments();

                if (gargs.Length > 0)
                {
                    Type messageType = iface.GetGenericArguments()[0];
                    RegisteredConsumers.Add(messageType, newConsumer(messageType, consumerDelegate));
                }
            }

            if (RegisteredConsumers.Count == 0)
            {
                throw new ArgumentException("Unable to find any messages to consume.", "consumer");
            }
        }

        public class RegisteredConsumer : IRegisteredConsumer
        {
            public RegisteredConsumer(Type messageType, Func<IConsumer> consumer)
            {
                MessageType = messageType;
                Queue = messageType.FullName;
                Consumer = consumer;
                ConsumeMethod = consumer.Invoke().GetType().GetMethod("Consume", new[] { messageType });
            }

            public Type MessageType { get; protected set; }
            public Func<IConsumer> Consumer { get; protected set; }
            public MethodInfo ConsumeMethod { get; protected set; }
            public string Queue { get; protected set; }

            public void Invoke(object message)
            {
                ConsumeMethod.Invoke(Consumer.Invoke(), new [] { message });
            }
        }

        public class RegisteredSubscriber : IRegisteredConsumer
        {
            public RegisteredSubscriber(Type messageType, Func<ISubscriber> subscriber)
            {
                MessageType = messageType;
                Subscriber = subscriber;
                ConsumeMethod = subscriber.Invoke().GetType().GetMethod("Consume", new[] { messageType });
                Queue = string.Format("{0}.{1}", messageType.FullName, Guid.NewGuid().ToString("n"));
            }

            public Type MessageType { get; protected set; }
            public Func<ISubscriber> Subscriber { get; protected set; }
            public MethodInfo ConsumeMethod { get; protected set; }
            public string Queue { get; protected set; }

            public void Invoke(object message)
            {
                ConsumeMethod.Invoke(Subscriber.Invoke(), new[] { message });
            }
        }
    }
}
