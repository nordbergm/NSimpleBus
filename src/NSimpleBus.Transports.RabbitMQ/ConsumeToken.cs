using System;
using RabbitMQ.Client;

namespace NSimpleBus.Transports.RabbitMQ
{
    public class ConsumeToken : IDisposable
    {
        private readonly string consumerTag;
        private readonly IModel model;
        private bool closed;

        public ConsumeToken(string consumerTag, IModel model)
        {
            this.consumerTag = consumerTag;
            this.model = model;
        }

        public void Close()
        {
            this.model.BasicCancel(consumerTag);
            this.closed = true;
        }

        public void Dispose()
        {
            if (!this.closed)
            {
                this.Close();
            }
        }
    }
}
