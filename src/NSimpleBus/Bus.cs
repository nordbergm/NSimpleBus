using System;
using System.Linq;
using NSimpleBus.Configuration;
using NSimpleBus.Transports;

namespace NSimpleBus
{
    public class Bus : IBus
    {
        private IBrokerConnection _connection;

        public Bus(IBrokerConfiguration configuration)
        {
            Configuration = configuration;

            foreach (var registeredConsumer in configuration.RegisteredConsumers.SelectMany(kvp => kvp.Value))
            {
                this.GetLiveConnection().Consume(registeredConsumer);
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

            if (this._connection == null)
            {
                this._connection = Configuration.ConnectionFactory.CreateConnection();
            }

            return this._connection;
        }

        public void Dispose()
        {
            if (this._connection != null && this._connection.IsOpen)
            {
                this._connection.Close();
            }
        }
    }
}
