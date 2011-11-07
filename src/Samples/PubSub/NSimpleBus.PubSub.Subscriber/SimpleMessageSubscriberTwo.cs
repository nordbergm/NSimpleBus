using System;
using NSimpleBus.PubSub.Common;

namespace NSimpleBus.PubSub.Subscriber
{
    public class SimpleMessageSubscriberTwo : Subscribes<SimpleMessage>.All
    {
        public void Consume(SimpleMessage message)
        {
            Console.WriteLine("Two: Received message {0} created {1}.", message.Id, message.Created);
        }
    }
}
