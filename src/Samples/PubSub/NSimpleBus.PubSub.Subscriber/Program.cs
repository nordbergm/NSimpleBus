using NSimpleBus.Configuration;
using System;

namespace NSimpleBus.PubSub.Subscriber
{
    class Program
    {
        static void Main(string[] args)
        {
            var bus = Bus.New(c =>
            {
                c.Broker("us1");
                c.Exchange("sample_pubsub");
                c.Credentials("rabbit", "rabbit");
                c.UseRabbitMq();
                c.PublishSubscribe();
                c.RegisterSubscriber(() => new SimpleMessageSubscriber());
            });

            Console.ReadLine();
        }
    }
}
