using System;
using System.Collections.Generic;
using System.Threading;
using NSimpleBus.Configuration;
using NSimpleBus.Serialization;
using NSimpleBus.Transports.RabbitMQ.Serialization;
using RabbitMQ.Client;

namespace NSimpleBus.Transports.RabbitMQ
{
    public class GroupedCallbackConsumer : IDisposable
    {
        private readonly Thread workerThread;
        private readonly IDictionary<string, QueueConsumer> queueConsumers;
        private readonly object lockObject = new object();
        private readonly Queue<QueueActivityConsumer.DeliverEventArgs> deliveryQueue = new Queue<QueueActivityConsumer.DeliverEventArgs>();

        public GroupedCallbackConsumer(IModel model, ISerializer serializer)
        {
            Model = model;
            IsRunning = true;
            Serializer = new MessageSerializer(serializer);
            queueConsumers = new Dictionary<string, QueueConsumer>();
            workerThread = new Thread(BackgroundConsume)
                               {
                                   IsBackground = true, 
                                   Name = "RabbitMQ Queue Consumer"
                               };
            workerThread.Start();
        }

        public bool IsRunning { get; private set; }
        public IModel Model { get; private set; }
        public MessageSerializer Serializer { get; private set; }

        private void BackgroundConsume()
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
                    this.CallbackWithEnvelope(args);
                }
            }
        }

        private void CallbackWithEnvelope(QueueActivityConsumer.DeliverEventArgs args)
        {
            if (this.queueConsumers.ContainsKey(args.Queue))
            {
                IMessageEnvelope<object> envelope = Serializer.DeserializeMessage(args);

                foreach (var registeredConsumer in this.queueConsumers[args.Queue].RegisteredConsumers)
                {
                    registeredConsumer.Invoke(envelope.Message);
                }
            }
        }

        public void ConsumeQueue(IRegisteredConsumer registeredConsumer)
        {
            if (!this.queueConsumers.ContainsKey(registeredConsumer.Queue))
            {
                var queueActivityConsumer = CreateAndSetupQueueConsumer(this.Model, registeredConsumer.Queue);

                QueueConsumer consumer = new QueueConsumer(
                    queueActivityConsumer,
                    registeredConsumer,
                    new ConsumeToken(this.Model.BasicConsume(registeredConsumer.Queue, false, queueActivityConsumer), this.Model)
                    );

                this.queueConsumers.Add(registeredConsumer.Queue, consumer);
            }
            else
            {
                queueConsumers[registeredConsumer.Queue].RegisteredConsumers.Add(registeredConsumer);
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
            IsRunning = false;

            foreach (var queueConsumer in queueConsumers)
            {
                queueConsumer.Value.ConsumeToken.Dispose();
            }
        }

        public void Dispose()
        {
            this.Close();
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
