using System;
using NSimpleBus.PubSub.Common;

namespace NSimpleBus.PubSub.Subscriber
{
    public class SimpleMessageSubscriber : Subscribes<SimpleMessage>.All
    {
        public void Consume(SimpleMessage message)
        {
            Console.WriteLine("Received message {0} created {1}.", message.Id, message.Created);
        }
    }
}
