using System;
using NSimpleBus.Configuration;
using NSimpleBus.Transports.RabbitMQ.Configuration;
using NSimpleBus.Transports.RabbitMQ.Serialization;
using RabbitMQ.Client;

namespace NSimpleBus.Transports.RabbitMQ
{
    public class BrokerConnection : IBrokerConnection
    {
        private readonly ICallbackConsumer callbackConsumer;
        private readonly IBrokerConfiguration configuration;
        private readonly IConnection connection;
        private readonly IModel model;
        private readonly IMessageSerializer serializer;

        public BrokerConnection(IConnection connection, IModel model, IBrokerConfiguration configuration, IMessageSerializer serializer, ICallbackConsumer callbackConsumer)
        {
            this.connection = connection;
            this.configuration = configuration;
            this.model = model;
            this.serializer = serializer;
            this.callbackConsumer = callbackConsumer;

            if (configuration.AutoConfigure != AutoConfigureMode.None)
            {
                this.AutoConfigureExchange(configuration, this.model);
            }
        }

        #region IBrokerConnection Members

        public bool IsOpen
        {
            get { return this.connection.IsOpen; }
        }

        public void Consume(IRegisteredConsumer registeredConsumer)
        {
            IRegisteredConsumer internalRegistererConsumer =
                new RegisteredConsumer(
                    registeredConsumer,
                    this.configuration.AutoConfigure);

            if (this.configuration.AutoConfigure != AutoConfigureMode.None)
            {
                this.AutoConfigureQueue(
                    internalRegistererConsumer.Queue,
                    this.configuration,
                    registeredConsumer.MessageType,
                    this.model);
            }

            this.callbackConsumer.ConsumeQueue(internalRegistererConsumer);
        }

        public void Publish<T>(IMessageEnvelope<T> message, string exchange) where T : class
        {
            lock (this.model)
            {
                IBasicProperties headers;
                byte[] body;
                string routingKey;

                this.serializer.SerializeMessage(message, this.model, out headers, out body, out routingKey);

                this.model.BasicPublish(
                    exchange,
                    routingKey,
                    headers,
                    body);
            }
        }

        public void Dispose()
        {
            if (this.IsOpen)
            {
                this.Close();
            }
        }

        public void Close()
        {
            if (!this.IsOpen)
            {
                throw new InvalidOperationException("The connection is not open and cannot be closed.");
            }

            if (this.callbackConsumer.IsRunning)
            {
                this.callbackConsumer.Close();
                this.callbackConsumer.Dispose();
            }

            if (this.model.IsOpen)
            {
                this.model.Close(200, "Goodbye");
                this.model.Dispose();
            }

            if (this.connection.IsOpen)
            {
                this.connection.Close();
                this.connection.Dispose();
            }
        }

        #endregion

        private void AutoConfigureExchange(IBrokerConfiguration config, IModel m)
        {
            string type;
            switch (config.AutoConfigure)
            {
                case AutoConfigureMode.PublishSubscribe:
                    type = "fanout";
                    break;

                case AutoConfigureMode.CompetingConsumer:
                    type = "direct";
                    break;

                default:
                    throw new NotSupportedException("The specified auto configuration mode is not supported.");
            }

            lock (m)
            {
                m.ExchangeDeclare(config.Exchange, type, true, false, null);
            }
        }

        private void AutoConfigureQueue(string queue, IBrokerConfiguration config, Type messageType, IModel m)
        {
            lock (m)
            {
                switch (config.AutoConfigure)
                {
                    case AutoConfigureMode.PublishSubscribe:
                        m.QueueDeclare(queue, true, true, true, null);
                        break;

                    case AutoConfigureMode.CompetingConsumer:
                        m.QueueDeclare(queue, true, false, false, null);
                        break;

                    default:
                        throw new NotSupportedException("The specified auto configuration mode is not supported.");
                }

                m.QueueBind(queue, config.Exchange, messageType.ToRoutingKey());
            }
        }
    }
}