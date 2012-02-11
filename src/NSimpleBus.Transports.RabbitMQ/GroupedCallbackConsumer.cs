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

        private readonly Thread _consumerWorkerThread;
        private readonly Thread _requeueWorkerThread;
        private readonly object _consumerLockObject = new object();
        private readonly object _requeueLockObject = new object();

        private readonly Queue<KeyValuePair<QueueActivityConsumer, QueueActivityConsumer.DeliverEventArgs>> _deliveryQueue = 
                                            new Queue<KeyValuePair<QueueActivityConsumer, QueueActivityConsumer.DeliverEventArgs>>();
        private readonly Queue<KeyValuePair<QueueActivityConsumer, QueueActivityConsumer.DeliverEventArgs>> _requeueQueue =
                                            new Queue<KeyValuePair<QueueActivityConsumer, QueueActivityConsumer.DeliverEventArgs>>();

        public GroupedCallbackConsumer(IModel model, IMessageSerializer serializer, IBrokerConfiguration config)
        {
            Model = model;
            Serializer = serializer;
            Config = config;
            IsRunning = true;
            QueueConsumers = new Dictionary<string, QueueConsumer>();

            this._consumerWorkerThread = new Thread(StartBackgroundConsume)
                               {
                                   IsBackground = true,
                                   Name = "RabbitMQ Consumer Queue"
                               };
            this._consumerWorkerThread.Start();

            this._requeueWorkerThread = new Thread(StartBackgroundRequeue)
                                {
                                    IsBackground = true,
                                    Name = "RabbitMQ Delayed Reject Queue"
                                };
            this._requeueWorkerThread.Start();
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
                lock (this._consumerLockObject)
                {
                    Monitor.Wait(this._consumerLockObject);
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

        private void StartBackgroundRequeue()
        {
            while (IsRunning)
            {
                lock (this._requeueLockObject)
                {
                    Monitor.Wait(this._requeueLockObject);
                }

                Thread.Sleep(this.Config.ReQueueDelay);

                while (this._requeueQueue.Count > 0)
                {
                    var args = this._requeueQueue.Dequeue();

                    try
                    {
                        args.Key.Model.BasicNack(args.Value.DeliveryTag, false, true);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            string.Format("An exception was thrown while trying to re-queue a message from queue {0}.",
                                          args.Value.Queue), ex);
                    }
                }
            }
        }

        private void CallbackWithMessage(QueueActivityConsumer sender, QueueActivityConsumer.DeliverEventArgs args)
        {
            if (this.QueueConsumers.ContainsKey(args.Queue) &&
                !this.QueueConsumers[args.Queue].ConsumeToken.IsClosed)
            {
                var registeredConsumer = this.QueueConsumers[args.Queue].RegisteredConsumer;
                var envelope = Serializer.DeserializeMessage(args);

                // Fire MessageReceived event
                Config.PipelineEvents.OnMessageReceived(new PipelineEventArgs(envelope));

                try
                {
                    var acceptance = registeredConsumer.Accept(envelope.Message);

                    if (acceptance == Acceptance.Accept)
                    {
                        if (!string.IsNullOrEmpty(envelope.UserName))
                        {
                            var resolvePrincipalArgs = new ResolvePrincipalEventArgs(envelope);

                            // Fire ResolvePrincipal event
                            Config.PipelineEvents.OnResolvePrincipal(resolvePrincipalArgs);

                            if (resolvePrincipalArgs.Principal != null)
                            {
                                Thread.CurrentPrincipal = resolvePrincipalArgs.Principal;
                            }
                        }

                        registeredConsumer.Invoke(envelope.Message);

                        sender.Model.BasicAck(args.DeliveryTag, false);

                        Log.InfoFormat("Accepted message {0}.",
                                       registeredConsumer.MessageType.FullName);

                        // Fire MessageConsumed event
                        Config.PipelineEvents.OnMessageConsumed(new PipelineEventArgs(envelope));
                    }
                    else if (acceptance == Acceptance.Requeue)
                    {
                        sender.Model.BasicNack(args.DeliveryTag, false, true);

                        Log.InfoFormat("Re-queued message {0}.",
                                       registeredConsumer.MessageType.FullName);
                    }
                    else if (acceptance == Acceptance.DelayedRequeue)
                    {
                        this._requeueQueue.Enqueue(
                            new KeyValuePair<QueueActivityConsumer, QueueActivityConsumer.DeliverEventArgs>(sender, args));

                        lock (this._requeueLockObject)
                        {
                            Monitor.Pulse(this._requeueLockObject);
                        }

                        Log.InfoFormat("Re-queued message {0} with delay.",
                                       registeredConsumer.MessageType.FullName);
                    }
                    else
                    {
                        sender.Model.BasicNack(args.DeliveryTag, false, false);

                        Log.WarnFormat("Rejected message {0}.",
                                       registeredConsumer.MessageType.FullName);
                    }
                }
                catch (Exception ex)
                {
                    this._requeueQueue.Enqueue(
                        new KeyValuePair<QueueActivityConsumer, QueueActivityConsumer.DeliverEventArgs>(sender, args));

                    lock (this._requeueLockObject)
                    {
                        Monitor.Pulse(this._requeueLockObject);
                    }

                    Log.Error(
                        string.Format("An exception was thrown while invoking the consumer."), ex);
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
                throw new InvalidOperationException("A consumer cannot be registered to a queue twice.");
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
            lock (this._consumerLockObject)
            {
                Monitor.Pulse(this._consumerLockObject);
            }
        }

        public void Close()
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("The callback consumer has already been closed.");
            }

            IsRunning = false;

            lock (this._consumerLockObject)
            {
                Monitor.Pulse(this._consumerLockObject);
            }

            this._consumerWorkerThread.Join();

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
                RegisteredConsumer = registeredConsumer;
            }

            public QueueActivityConsumer Consumer { get; private set; }
            public IRegisteredConsumer RegisteredConsumer { get; set; }
            public ConsumeToken ConsumeToken { get; private set; }
        }
    }
}
