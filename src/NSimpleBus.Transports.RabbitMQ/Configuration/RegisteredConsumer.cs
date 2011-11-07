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
            this.Queue = ToInternalQueueName(this.Other.Queue, this.Other.ConsumerType, autoConfigureMode);
        }

        public IRegisteredConsumer Other { get; private set; }
        public AutoConfigureMode AutoConfigureMode { get; private set; }
        
        #region IRegisteredConsumer Members

        public string Queue { get; private set; }

        public Type ConsumerType
        {
            get { return this.Other.ConsumerType; }
        }

        public bool AutoDeleteQueue
        {
            get { return this.Other.AutoDeleteQueue; }
        }

        public Type MessageType
        {
            get { return this.Other.MessageType; }
        }

        public void Invoke(object message)
        {
            this.Other.Invoke(message);
        }

        public Acceptance Accept(object message)
        {
            return this.Other.Accept(message);
        }

        #endregion

        private static string ToInternalQueueName(string queue, Type consumerType, AutoConfigureMode autoConfigureMode)
        {
            string iq = string.Format("{0}.{1}", consumerType.FullName, queue);

            // Max queue length is 255 characters in RabbitMQ
            if (iq.Length > 255)
            {
                iq = iq.Remove(255);
            }

            if (autoConfigureMode == AutoConfigureMode.None)
            {
                return iq;
            }

            return string.Format("{0}.{1}", autoConfigureMode, iq);
        }
    }
}