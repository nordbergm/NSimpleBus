using System;
using System.Collections.Generic;
using NSimpleBus.Configuration;
using NSimpleBus.Transports.RabbitMQ.Serialization;
using RabbitMQ.Client;

namespace NSimpleBus.Transports.RabbitMQ
{
    public interface ICallbackConsumer : IDisposable
    {
        bool IsRunning { get; }
        IModel Model { get; }
        IMessageSerializer Serializer { get; }
        IDictionary<string, GroupedCallbackConsumer.QueueConsumer> QueueConsumers { get; }
        void ConsumeQueue(IRegisteredConsumer registeredConsumer);
        void Close();
    }
}