using System;
using RabbitMQ.Client;

namespace NSimpleBus.Transports.RabbitMQ
{
    public class ConsumeToken : IDisposable
    {
        public ConsumeToken(string consumerTag, IModel model)
        {
            ConsumerTag = consumerTag;
            Model = model;
        }

        public string ConsumerTag { get; private set; }
        public IModel Model { get; private set; }
        public bool IsClosed { get; private set; }

        public void Close()
        {
            this.Model.BasicCancel(ConsumerTag);
            this.IsClosed = true;
        }

        public void Dispose()
        {
            if (!this.IsClosed)
            {
                this.Close();
            }
        }
    }
}
