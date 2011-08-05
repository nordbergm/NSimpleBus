using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSimpleBus.PubSub.Common;

namespace NSimpleBus.PubSub.Subscriber
{
    public class SimpleMessageConsumer : Consumes<SimpleMessage>.All
    {
        public void Consume(SimpleMessage message)
        {
            Console.WriteLine("Received message {0} created {1}.", message.Id, message.Created);
        }
    }
}
