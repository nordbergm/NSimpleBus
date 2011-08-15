﻿using System;
using log4net;
using NSimpleBus.Configuration;
using NSimpleBus.Transports.RabbitMQ.Configuration;
using NSimpleBus.Transports.RabbitMQ.Serialization;
using RabbitMQ.Client;

namespace NSimpleBus.Transports.RabbitMQ
{
    public class BrokerConnection : IBrokerConnection
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (ILog));

        private readonly ICallbackConsumer _callbackConsumer;
        private readonly IBrokerConfiguration _configuration;
        private readonly IConnection _connection;
        private readonly IModel _model;
        private readonly IMessageSerializer _serializer;

        public BrokerConnection(IConnection connection, IModel model, IBrokerConfiguration configuration, IMessageSerializer serializer, ICallbackConsumer callbackConsumer)
        {
            this._connection = connection;
            this._configuration = configuration;
            this._model = model;
            this._serializer = serializer;
            this._callbackConsumer = callbackConsumer;
        }

        #region IBrokerConnection Members

        public bool IsOpen
        {
            get { return this._connection.IsOpen; }
        }

        public void Consume(IRegisteredConsumer registeredConsumer)
        {
            IRegisteredConsumer internalRegisteredConsumer =
                new RegisteredConsumer(
                    registeredConsumer,
                    this._configuration.AutoConfigure);

            if (this._configuration.AutoConfigure != AutoConfigureMode.None)
            {
                this.AutoConfigureExchange(this._configuration, registeredConsumer.MessageType, this._model);
            }

            if (this._configuration.AutoConfigure != AutoConfigureMode.None)
            {
                this.AutoConfigureQueue(
                    internalRegisteredConsumer.Queue,
                    this._configuration,
                    registeredConsumer.MessageType,
                    this._model);
            }

            this._callbackConsumer.ConsumeQueue(internalRegisteredConsumer);
        }

        public void Publish<T>(IMessageEnvelope<T> message) where T : class
        {
            lock (this._model)
            {
                IBasicProperties headers;
                byte[] body;
                string routingKey;

                this._serializer.SerializeMessage(message, this._model, out headers, out body, out routingKey);

                this._model.BasicPublish(
                    _configuration.InternalExchange(typeof(T)),
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

            if (this._callbackConsumer.IsRunning)
            {
                this._callbackConsumer.Close();
                this._callbackConsumer.Dispose();

                Log.Info("Consumer has been closed and disposed.");
            }

            if (this._model.IsOpen)
            {
                this._model.Close(200, "Goodbye");
                this._model.Dispose();

                Log.Info("Model has been closed and disposed.");
            }

            if (this._connection.IsOpen)
            {
                this._connection.Close(200, "Goodbye");
                this._connection.Dispose();

                Log.Info("Connection has been closed and disposed.");
            }
        }

        #endregion

        private void AutoConfigureExchange(IBrokerConfiguration config, Type messageType, IModel m)
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
                m.ExchangeDeclare(config.InternalExchange(messageType), type, true, false, null);
                Log.InfoFormat("Exchange '{0}' has been auto-configured as '{1}'.", config.InternalExchange(messageType), type);
            }
        }

        private void AutoConfigureQueue(string queue, IBrokerConfiguration config, Type messageType, IModel m)
        {
            lock (m)
            {
                string exchange;
                switch (config.AutoConfigure)
                {
                    case AutoConfigureMode.PublishSubscribe:
                        m.QueueDeclare(queue, true, true, true, null);
                        Log.InfoFormat("Queue '{0}' has been auto-configured as exclusive and auto-delete.", queue);
                        break;

                    case AutoConfigureMode.CompetingConsumer:
                        m.QueueDeclare(queue, true, false, false, null);
                        Log.InfoFormat("Queue '{0}' has been auto-configured as non exclusive and persistent.", queue);
                        break;

                    default:
                        throw new NotSupportedException("The specified auto configuration mode is not supported.");
                }

                m.QueueBind(queue, config.InternalExchange(messageType), messageType.ToRoutingKey());
                Log.InfoFormat("Queue '{0}' has been bound to exchange '{1}'.", queue, config.InternalExchange(messageType));
            }
        }
    }
}