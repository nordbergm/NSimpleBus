using System;
using System.Linq;
using log4net;
using NSimpleBus.Configuration;
using NSimpleBus.Transports;

namespace NSimpleBus
{
    public class Bus : IBus
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (Bus));

        private IBrokerConnection _connection;

        public Bus(IBrokerConfiguration configuration)
        {
            this.Configuration = configuration;
            this._connection = this.GetLiveConnection();
        }

        #region IBus Members

        public IBrokerConfiguration Configuration { get; private set; }

        public void Publish<T>(T message) where T : class
        {
            if (string.IsNullOrEmpty(this.Configuration.Exchange))
            {
                throw new InvalidOperationException("You need to configure an exchange to publish messages.");
            }

            try
            {
                var envelope = new MessageEnvelope<T>(message);

                this.GetLiveConnection().Publish(envelope);

                Log.InfoFormat("Published {0} to exchange {1}.", typeof (T).FullName, this.Configuration.Exchange);
            }
            catch (Exception ex)
            {
                Log.Error(
                    string.Format("An exception was thrown while trying to publish {0} to exchange {1}.",
                                  typeof (T).FullName,
                                  this.Configuration.Exchange),
                    ex);
                throw;
            }
        }

        public void Dispose()
        {
            if (this._connection != null && this._connection.IsOpen)
            {
                this._connection.Close();
            }
        }

        #endregion

        public static Bus New(Action<IBrokerConfiguration> init)
        {
            var config = new BrokerConfiguration();
            init(config);

            return new Bus(config);
        }

        public IBrokerConnection GetLiveConnection()
        {
            if (this._connection == null)
            {
                this._connection = this.Configuration.ConnectionFactory.CreateConnection();
                this.RegisterConsumers(this._connection, this.Configuration);
            }

            if (!this._connection.IsOpen)
            {
                this._connection.Dispose();
                this._connection = this.Configuration.ConnectionFactory.CreateConnection();
                this.RegisterConsumers(this._connection, this.Configuration);
            }

            return this._connection;
        }

        private void RegisterConsumers(IBrokerConnection conn, IBrokerConfiguration config)
        {
            try
            {
                foreach (
                    IRegisteredConsumer registeredConsumer in config.RegisteredConsumers.SelectMany(kvp => kvp.Value))
                {
                    conn.Consume(registeredConsumer);

                    Log.InfoFormat("Started consuming {0} from queue {1}.", registeredConsumer.MessageType.FullName,
                                   registeredConsumer.Queue);
                }
            }
            catch (Exception ex)
            {
                Log.Error("An exception was thrown while initializing the broker connection.", ex);
                throw;
            }
        }
    }
}