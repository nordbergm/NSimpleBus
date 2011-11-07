using System;
using System.Collections.Generic;
using System.Threading;
using log4net;
using NSimpleBus.Configuration;
using NSimpleBus.Transports.RabbitMQ.Serialization;
using RabbitMQ.Client;

namespace NSimpleBus.Transports.RabbitMQ
{
    public class GroupedCallbackConsumer : ICallbackConsumer
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (GroupedCallbackConsumer));

        private readonly Thread _workerThread;
        private readonly object _lockObject = new object();
        private readonly Queue<KeyValuePair<QueueActivityConsumer, QueueActivityConsumer.DeliverEventArgs>> _deliveryQueue = 
                                            new Queue<KeyValuePair<QueueActivityConsumer, QueueActivityConsumer.DeliverEventArgs>>();

        public GroupedCallbackConsumer(IModel model, IMessageSerializer serializer, IBrokerConfiguration config)
        {
            Model = model;
            Serializer = serializer;
            Config = config;
            IsRunning = true;
            QueueConsumers = new Dictionary<string, QueueConsumer>();

            this._workerThread = new Thread(StartBackgroundConsume)
                               {
                                   IsBackground = true, 
                                   Name = "RabbitMQ Queue Consumer"
                               };
            this._workerThread.Start();
        }

        public bool IsRunning { get; private set; }
        public IModel Model { get; private set; }
        public IMessageSerializer Serializer { get; private set; }
        public IBrokerConfiguration Config { get; set; }
        public IDictionary<string, QueueConsumer> QueueConsumers { get; private set; }

        private void StartBackgroundConsume()
        {
            while(IsRunning)
            {
                lock (this._lockObject)
                {
                    Monitor.Wait(this._lockObject);
                }

                while (this._deliveryQueue.Count > 0)
                {
                    var args = this._deliveryQueue.Dequeue();

                    try
                    {
                        this.CallbackWithMessage(args.Key, args.Value);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            string.Format("An exception was thrown while trying to consume a message from queue {0}.",
                                          args.Value.Queue), ex);
                    }
                }
            }
        }

        private void CallbackWithMessage(QueueActivityConsumer sender, QueueActivityConsumer.DeliverEventArgs args)
        {
            bool handled = false;
            
            if (this.QueueConsumers.ContainsKey(args.Queue) &&
                !this.QueueConsumers[args.Queue].ConsumeToken.IsClosed)
            {
                IMessageEnvelope<object> envelope = Serializer.DeserializeMessage(args);

                if (!string.IsNullOrEmpty(envelope.UserName))
                {
                    Thread.CurrentPrincipal = Config.CreatePrincipal(envelope.UserName);
                }

                foreach (var registeredConsumer in this.QueueConsumers[args.Queue].RegisteredConsumers)
                {
                    try
                    {
                        registeredConsumer.Invoke(envelope.Message);
                        Log.InfoFormat("Successfully received and consumed message {0}.",
                                       registeredConsumer.MessageType.FullName);


                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            string.Format("An exception was thrown while invoking the consumer."), ex);
                    }

                    handled = true;
                }
            }

            if(handled)
            {
                sender.Model.BasicAck(args.DeliveryTag, false);
            }
            else
            {
                sender.Model.BasicNack(args.DeliveryTag, false, true);
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

            this._deliveryQueue.Enqueue(new KeyValuePair<QueueActivityConsumer, QueueActivityConsumer.DeliverEventArgs>(consumer, e));
            lock (this._lockObject)
            {
                Monitor.Pulse(this._lockObject);
            }
        }

        public void Close()
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("The callback consumer has already been closed.");
            }

            IsRunning = false;

            lock (this._lockObject)
            {
                Monitor.Pulse(this._lockObject);
            }

            this._workerThread.Join();

            foreach (var queueConsumer in QueueConsumers)
            {
                queueConsumer.Value.ConsumeToken.Close();
                queueConsumer.Value.ConsumeToken.Dispose();
            }

            Log.InfoFormat("Consumer has been closed.");
        }

        public void Dispose()
        {
            if (IsRunning)
            {
                this.Close();
            }

            Log.InfoFormat("Consumer has been disposed.");
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
