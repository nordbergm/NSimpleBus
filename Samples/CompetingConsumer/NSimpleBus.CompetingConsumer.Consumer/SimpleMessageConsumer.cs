using System;
using NSimpleBus.CompetingConsumer.Messages;

namespace NSimpleBus.CompetingConsumer.Consumer
{
    public class SimpleMessageConsumer : Consumes<SimpleMessage>.All
    {
        public void Consume(SimpleMessage message)
        {
            Console.WriteLine("Received message {0} created {1}.", message.Id, message.Created);
        }
    }
}
