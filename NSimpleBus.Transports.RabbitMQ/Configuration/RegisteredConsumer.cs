using System;
using NSimpleBus.Configuration;

namespace NSimpleBus.Transports.RabbitMQ.Configuration
{
    public class RegisteredConsumer : IRegisteredConsumer
    {
        public RegisteredConsumer(IRegisteredConsumer other, AutoConfigureMode autoConfigureMode)
        {
            this.Other = other;
            this.AutoConfigureMode = autoConfigureMode;
            this.Queue = ToInternalQueueName(this.Other.Queue, autoConfigureMode);
        }

        public IRegisteredConsumer Other { get; private set; }
        public AutoConfigureMode AutoConfigureMode { get; private set; }

        #region IRegisteredConsumer Members

        public string Queue { get; private set; }

        public IConsumer Consumer
        {
            get { return this.Other.Consumer; }
        }

        public Type MessageType
        {
            get { return this.Other.MessageType; }
        }

        public void Invoke(object message)
        {
            this.Other.Invoke(message);
        }

        #endregion

        private static string ToInternalQueueName(string queue, AutoConfigureMode autoConfigureMode)
        {
            if (autoConfigureMode == AutoConfigureMode.None)
            {
                return queue;
            }

            return string.Format("{0}_{1}", autoConfigureMode, queue);
        }
    }
}