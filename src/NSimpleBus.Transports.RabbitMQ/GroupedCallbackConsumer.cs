using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NSimpleBus.Configuration;
using NSimpleBus.Transports.RabbitMQ.Serialization;
using RabbitMQ.Client;

namespace NSimpleBus.Transports.RabbitMQ
{
    public class GroupedCallbackConsumer : ICallbackConsumer
    {
        private readonly Thread workerThread;
        private readonly object lockObject = new object();
        private readonly Queue<QueueActivityConsumer.DeliverEventArgs> deliveryQueue = new Queue<QueueActivityConsumer.DeliverEventArgs>();

        public GroupedCallbackConsumer(IModel model, IMessageSerializer serializer)
        {
            Model = model;
            Serializer = serializer;
            IsRunning = true;
            QueueConsumers = new Dictionary<string, QueueConsumer>();

            workerThread = new Thread(StartBackgroundConsume)
                               {
                                   IsBackground = true, 
                                   Name = "RabbitMQ Queue Consumer"
                               };
            workerThread.Start();
        }

        public bool IsRunning { get; private set; }
        public IModel Model { get; private set; }
        public IMessageSerializer Serializer { get; private set; }
        public IDictionary<string, QueueConsumer> QueueConsumers { get; private set; }

        private void StartBackgroundConsume()
        {
            while(IsRunning)
            {
                lock (lockObject)
                {
                    Monitor.Wait(lockObject);
                }

                while (deliveryQueue.Count > 0)
                {
                    var args = deliveryQueue.Dequeue();
                    this.CallbackWithMessage(args);
                }
            }
        }

        private void CallbackWithMessage(QueueActivityConsumer.DeliverEventArgs args)
        {
            if (this.QueueConsumers.ContainsKey(args.Queue) && !this.QueueConsumers[args.Queue].ConsumeToken.IsClosed)
            {
                IMessageEnvelope<object> envelope = Serializer.DeserializeMessage(args);

                foreach (var registeredConsumer in this.QueueConsumers[args.Queue].RegisteredConsumers)
                {
                    registeredConsumer.Invoke(envelope.Message);
                }
            }
        }

        public void ConsumeQueue(IRegisteredConsumer registeredConsumer)
        {
            if (!this.QueueConsumers.ContainsKey(registeredConsumer.Queue))
            {
                var queueActivityConsumer = CreateAndSetupQueueConsumer(this.Model, registeredConsumer.Queue);

                QueueConsumer consumer = new QueueConsumer(
                    queueActivityConsumer,
                    registeredConsumer,
                    new ConsumeToken(this.Model.BasicConsume(registeredConsumer.Queue, false, queueActivityConsumer), this.Model)
                    );

                this.QueueConsumers.Add(registeredConsumer.Queue, consumer);
            }
            else
            {
                QueueConsumers[registeredConsumer.Queue].RegisteredConsumers.Add(registeredConsumer);
            }
        }

        private QueueActivityConsumer CreateAndSetupQueueConsumer(IModel model, string queue)
        {
            var queueConsumer = new QueueActivityConsumer(model, queue);
            queueConsumer.Deliver += OnQueueDeliver;

            return queueConsumer;
        }

        private void OnQueueDeliver(object sender, QueueActivityConsumer.DeliverEventArgs e)
        {
            QueueActivityConsumer consumer = (QueueActivityConsumer) sender;
            
            deliveryQueue.Enqueue(e);
            lock (lockObject)
            {
                Monitor.Pulse(lockObject);
            }

            consumer.Model.BasicAck(e.DeliveryTag, false);
        }

        public void Close()
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("The callback consumer has already been closed.");
            }

            IsRunning = false;

            lock (this.lockObject)
            {
                Monitor.Pulse(this.lockObject);
            }

            this.workerThread.Join();

            foreach (var queueConsumer in QueueConsumers)
            {
                queueConsumer.Value.ConsumeToken.Close();
                queueConsumer.Value.ConsumeToken.Dispose();
            }
        }

        public void Dispose()
        {
            if (IsRunning)
            {
                this.Close();
            }
        }

        public class QueueConsumer
        {
            public QueueConsumer(QueueActivityConsumer consumer, IRegisteredConsumer registeredConsumer, ConsumeToken consumeToken)
            {
                Consumer = consumer;
                ConsumeToken = consumeToken;
                RegisteredConsumers = new List<IRegisteredConsumer> { registeredConsumer };
            }

            public QueueActivityConsumer Consumer { get; private set; }
            public IList<IRegisteredConsumer> RegisteredConsumers { get; set; }
            public ConsumeToken ConsumeToken { get; private set; }
        }
    }
}
