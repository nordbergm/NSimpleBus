using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using log4net;
using NSimpleBus.Serialization;
using NSimpleBus.Transports;

namespace NSimpleBus.Configuration
{
    public class BrokerConfiguration : IBrokerConfiguration
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (BrokerConfiguration));

        public BrokerConfiguration()
        {
            this.RegisteredConsumers = new Dictionary<Type, IList<IRegisteredConsumer>>();
            this.AutoConfigure = AutoConfigureMode.None;
            this.CreatePrincipal = n => new GenericPrincipal(new GenericIdentity(n), new string[0]);
        }

        #region IBrokerConfiguration Members

        public string UserName { get; set; }
        public string Password { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; }
        public CreatePrincipalDelegate CreatePrincipal { get; set; }
        public ResolveQueueNameDelegate ResolveQueueName { get; set; }
        public ISerializer Serializer { get; set; }
        public string Exchange { get; set; }
        public string VirtualHost { get; set; }
        public AutoConfigureMode AutoConfigure { get; set; }
        public IDictionary<Type, IList<IRegisteredConsumer>> RegisteredConsumers { get; set; }
        public IBrokerConnectionFactory ConnectionFactory { get; set; }

        public void RegisterConsumers(Assembly assembly, string nameSpace = null, Func<Type, IConsumer> resolver = null)
        {
            RegisterConsumers(new[] {assembly}, nameSpace != null ? new[] {nameSpace} : null, resolver);
        }

        public void RegisterConsumers(Assembly[] assemblies, string[] nameSpaces = null,
                                      Func<Type, IConsumer> resolver = null)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException("assembly");
            }

            try
            {
                foreach (Type type in assemblies.SelectMany(a => a.GetTypes())
                    .Where(t => (nameSpaces == null || nameSpaces.Contains(t.Namespace)) &&
                                t.GetInterfaces().Contains(typeof (IConsumer))))
                {
                    Type consumerType = type;
                    Func<Type, IConsumer> r = resolver;
                    this.RegisterConsumer(() => r != null
                                                    ? r(consumerType)
                                                    : (IConsumer) Activator.CreateInstance(consumerType),
                                          (t, c) => new RegisteredConsumer(t, c, this.ResolveQueueName));
                }
            }
            catch (Exception ex)
            {
                Log.Error("An exception occured while registering consumers.", ex);
                throw;
            }
        }

        public void RegisterSubscribers(Assembly assembly, string nameSpace = null,
                                        Func<Type, ISubscriber> resolver = null)
        {
            RegisterSubscribers(new[] {assembly}, nameSpace != null ? new[] {nameSpace} : null, resolver);
        }

        public void RegisterSubscribers(Assembly[] assemblies, string[] nameSpaces = null,
                                        Func<Type, ISubscriber> resolver = null)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException("assembly");
            }

            try
            {
                foreach (Type type in assemblies.SelectMany(a => a.GetTypes())
                    .Where(
                        t =>
                        (nameSpaces == null || nameSpaces.Contains(t.Namespace)) &&
                        t.GetInterfaces().Contains(typeof (ISubscriber))))
                {
                    Type consumerType = type;
                    Func<Type, ISubscriber> r = resolver;
                    this.RegisterConsumer(
                        () => r != null
                                  ? r(consumerType)
                                  : (ISubscriber) Activator.CreateInstance(consumerType),
                        (t, c) => new RegisteredSubscriber(t, c, this.ResolveQueueName));
                }
            }
            catch (Exception ex)
            {
                Log.Error("An exception occured while registering subscribers.", ex);
                throw;
            }
        }

        public void RegisterSubscriber(Func<ISubscriber> subscriberDelegate)
        {
            this.RegisterConsumer(subscriberDelegate, (t, c) => new RegisteredSubscriber(t, c, this.ResolveQueueName));
        }

        public void RegisterConsumer(Func<IConsumer> consumerDelegate)
        {
            this.RegisterConsumer(consumerDelegate, (t, c) => new RegisteredConsumer(t, c, this.ResolveQueueName));
        }

        #endregion

        private void RegisterConsumer<T>(Func<T> consumerDelegate, Func<Type, Func<T>, IRegisteredConsumer> newConsumer)
            where T : class
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
                    string.Format("An exception was thrown when invoking the consumer delegate '{0}.",
                                  consumerDelegate.GetType().GetGenericArguments()[0].FullName), ex);
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

                Type[] gargs = iface.GetGenericArguments();

                if (gargs.Length > 0)
                {
                    Type messageType = iface.GetGenericArguments()[0];

                    if (!this.RegisteredConsumers.ContainsKey(messageType))
                    {
                        this.RegisteredConsumers.Add(messageType, new List<IRegisteredConsumer>());
                    }

                    this.RegisteredConsumers[messageType].Add(newConsumer(messageType, consumerDelegate));

                    Log.InfoFormat("Registered {0} as {1} for message type {2}.", consumer.GetType().FullName,
                                   typeof (T).Name, messageType.FullName);
                }
            }

            if (this.RegisteredConsumers.Count == 0)
            {
                throw new ArgumentException("Unable to find any messages to consume.", "consumer");
            }
        }

        #region Nested type: RegisteredConsumer

        public class RegisteredConsumer : IRegisteredConsumer
        {
            public RegisteredConsumer(Type messageType, Func<IConsumer> consumer, ResolveQueueNameDelegate queueNameResolver)
            {
                this.MessageType = messageType;
                this.Queue = queueNameResolver != null ? 
                                queueNameResolver(messageType, typeof(IConsumer)) : 
                                messageType.FullName;
                this.Consumer = consumer;
                this.AutoDeleteQueue = false;
                this.ConsumeMethods = new List<MethodInfo>();

                Type consumerType = consumer.Invoke().GetType();
                Type[] interfaces = consumerType.GetInterfaces();
                foreach (Type iface in interfaces.Where(i => i.GetInterfaces().Contains(typeof (IConsumer))))
                {
                    var method = iface.GetMethod("Consume", new[] {messageType}) ??
                                 consumerType.GetMethod("Consume", new[] {messageType});

                    if (method == null)
                    {
                        throw new MissingMethodException(consumerType.Name, "Consume");
                    }

                    this.ConsumeMethods.Add(method);
                }
            }

            public Func<IConsumer> Consumer { get; protected set; }
            public IList<MethodInfo> ConsumeMethods { get; protected set; }

            #region IRegisteredConsumer Members

            public Type MessageType { get; protected set; }
            public string Queue { get; protected set; }
            public bool AutoDeleteQueue { get; private set; }

            public void Invoke(object message)
            {
                foreach (MethodInfo method in this.ConsumeMethods)
                {
                    method.Invoke(this.Consumer.Invoke(), new[] {message});
                }
            }

            #endregion
        }

        #endregion

        #region Nested type: RegisteredSubscriber

        public class RegisteredSubscriber : IRegisteredConsumer
        {
            public RegisteredSubscriber(Type messageType, Func<ISubscriber> subscriber, ResolveQueueNameDelegate queueNameResolver)
            {
                this.MessageType = messageType;
                this.Subscriber = subscriber;
                this.Queue = queueNameResolver != null ? 
                                queueNameResolver(messageType, typeof(ISubscriber)) : 
                                string.Format("{0}.{1}", messageType.FullName, Guid.NewGuid().ToString("n"));
                this.AutoDeleteQueue = queueNameResolver == null;
                this.ConsumeMethods = new List<MethodInfo>();

                Type subscriberType = subscriber.Invoke().GetType();
                Type[] interfaces = subscriberType.GetInterfaces();
                foreach (Type iface in interfaces.Where(i => i.GetInterfaces().Contains(typeof (ISubscriber))))
                {
                    var method = iface.GetMethod("Consume", new[] { messageType }) ??
                                 subscriberType.GetMethod("Consume", new[] { messageType });

                    if (method == null)
                    {
                        throw new MissingMethodException(subscriberType.Name, "Consume");
                    }

                    this.ConsumeMethods.Add(method);
                }
            }

            public Func<ISubscriber> Subscriber { get; protected set; }
            public IList<MethodInfo> ConsumeMethods { get; protected set; }

            #region IRegisteredConsumer Members

            public Type MessageType { get; protected set; }
            public string Queue { get; protected set; }
            public bool AutoDeleteQueue { get; private set; }

            public void Invoke(object message)
            {
                foreach (MethodInfo method in this.ConsumeMethods)
                {
                    method.Invoke(this.Subscriber.Invoke(), new[] {message});
                }
            }

            #endregion
        }

        #endregion
    }
}