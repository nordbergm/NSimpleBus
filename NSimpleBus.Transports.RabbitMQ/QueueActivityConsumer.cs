using System;
using RabbitMQ.Client;

namespace NSimpleBus.Transports.RabbitMQ
{
    public class QueueActivityConsumer : IBasicConsumer
    {
        public QueueActivityConsumer(IModel model, string queue)
        {
            Model = model;
            Queue = queue;
        }

        #region Events

        public event EventHandler<ConsumerTagEventArgs> Cancel;
        public event EventHandler<ConsumerTagEventArgs> CancelOk;
        public event EventHandler<ConsumerTagEventArgs> ConsumeOk;
        public event EventHandler<DeliverEventArgs> Deliver;
        public event EventHandler<ModelShutdownEventArgs> ModelShutdown;

        #endregion

        #region Methods

        public void HandleBasicCancel(string consumerTag)
        {
            if (Cancel != null)
            {
                Cancel(this, new ConsumerTagEventArgs(consumerTag));
            }
        }

        public void HandleBasicCancelOk(string consumerTag)
        {
            if (CancelOk != null)
            {
                CancelOk(this, new ConsumerTagEventArgs(consumerTag));
            }
        }

        public void HandleBasicConsumeOk(string consumerTag)
        {
            if (ConsumeOk != null)
            {
                ConsumeOk(this, new ConsumerTagEventArgs(consumerTag));
            }
        }

        public void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, byte[] body)
        {
            if (Deliver != null)
            {
                Deliver(this, new DeliverEventArgs(consumerTag, deliveryTag, redelivered, exchange, this.Queue, routingKey, properties, body));
            }
        }

        public void HandleModelShutdown(IModel model, ShutdownEventArgs reason)
        {
            if (ModelShutdown != null)
            {
                ModelShutdown(this, new ModelShutdownEventArgs(model, reason));
            }
        }

        #endregion

        public IModel Model { get; private set; }
        public string Queue { get; private set; }

        #region Event Args

        public class ConsumerTagEventArgs : EventArgs
        {
            public ConsumerTagEventArgs(string consumerTag)
            {
                ConsumerTag = consumerTag;
            }

            public string ConsumerTag { get; private set; }
        }

        public class DeliverEventArgs : EventArgs
        {
            public DeliverEventArgs(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string queue, string routingKey, IBasicProperties properties, byte[] body)
            {
                ConsumerTag = consumerTag;
                DeliveryTag = deliveryTag;
                Redelivered = redelivered;
                Exchange = exchange;
                Queue = queue;
                RoutingKey = routingKey;
                Properties = properties;
                Body = body;
            }

            public string ConsumerTag { get; private set; }
            public ulong DeliveryTag { get; private set; }
            public bool Redelivered { get; private set; }
            public string Exchange { get; private set; }
            public string Queue { get; private set; }
            public string RoutingKey { get; private set; }
            public IBasicProperties Properties { get; private set; }
            public byte[] Body { get; private set; }
        }

        public class ModelShutdownEventArgs : EventArgs
        {
            public ModelShutdownEventArgs(IModel model, ShutdownEventArgs reason)
            {
                Model = model;
                Reason = reason;
            }

            public IModel Model { get; private set; }
            public ShutdownEventArgs Reason { get; private set; }
        }

        #endregion
    }
}
