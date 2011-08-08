using System;
using NSimpleBus.Configuration;
using NSimpleBus.Transports;

namespace NSimpleBus
{
    public class Bus : IBus
    {
        private IBrokerConnection connection;

        public Bus(IBrokerConfiguration configuration)
        {
            Configuration = configuration;

            foreach (var registeredConsumer in configuration.RegisteredConsumers)
            {
                this.GetLiveConnection().Consume(registeredConsumer.Value);
            }
        }

        public IBrokerConfiguration Configuration { get; private set; }

        public void Publish<T>(T message) where T : class
        {
            if (string.IsNullOrEmpty(Configuration.Exchange))
            {
                throw new InvalidOperationException("You need to configure an exchange to publish messages.");
            }

            var envelope = new MessageEnvelope<T>(message);

            GetLiveConnection().Publish(envelope, Configuration.Exchange);
        }

        public static Bus New(Action<IBrokerConfiguration> init)
        {
            BrokerConfiguration config = new BrokerConfiguration();
            init(config);

            return new Bus(config);
        }

        private IBrokerConnection GetLiveConnection()
        {
            // TODO: Make this nice and fault tolerant to dropped connections
            // BUG: Leaking connections

            if (this.connection == null)
            {
                this.connection = Configuration.ConnectionFactory.CreateConnection();
            }

            return connection;
        }

        public void Dispose()
        {
            if (this.connection != null && this.connection.IsOpen)
            {
                this.connection.Close();
            }
        }
    }
}
