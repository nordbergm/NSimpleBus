using System;
using NSimpleBus.Configuration;
using NSimpleBus.Transports.RabbitMQ.Configuration;
using NSimpleBus.Transports.RabbitMQ.Serialization;
using RabbitMQ.Client;

namespace NSimpleBus.Transports.RabbitMQ
{
    public class BrokerConnection : IBrokerConnection
    {
        private readonly IConnection connection;
        private readonly IBrokerConfiguration configuration;
        private readonly IModel model;
        private readonly MessageSerializer serializer;
        private readonly GroupedCallbackConsumer callbackConsumer;

        public BrokerConnection(IConnection connection, IBrokerConfiguration configuration)
        {
            this.connection = connection;
            this.configuration = configuration;
            this.model = connection.CreateModel();
            this.serializer = new MessageSerializer(configuration.Serializer);
            this.callbackConsumer = new GroupedCallbackConsumer(this.model, configuration.Serializer);

            if (configuration.AutoConfigure != AutoConfigureMode.None)
            {
                AutoConfigureExchange(configuration.Exchange, this.model, configuration.AutoConfigure);
            }
        }

        private void AutoConfigureExchange(string exchange, IModel model, AutoConfigureMode autoConfigureMode)
        {
            string type;
            switch (autoConfigureMode)
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

            lock (model)
            {
                model.ExchangeDeclare(exchange, type, true, false, null);
            }
        }



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
                AutoConfigureQueue(
                        internalRegistererConsumer.Queue, 
                        this.configuration.Exchange, 
                        registeredConsumer.MessageType, 
                        this.model);
            }

            this.callbackConsumer.ConsumeQueue(internalRegistererConsumer);
        }

        private void AutoConfigureQueue(string queue, string exchange, Type messageType, IModel model)
        {
            lock (model)
            {
                model.QueueDeclare(queue, true, false, false, null);
                model.QueueBind(queue, exchange, messageType.ToRoutingKey());
            }
        }

        public void Publish<T>(IMessageEnvelope<T> message, string exchange) where T : class
        {
            lock (this.model)
            {
                IBasicProperties headers;
                byte[] body;
                string routingKey;

                serializer.SerializeMessage(message, this.model, out headers, out body, out routingKey);

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

            callbackConsumer.Close();
            callbackConsumer.Dispose();
            model.Close(200, "Goodbye");
            model.Dispose();
            connection.Close();
            connection.Dispose();
        }
    }
}