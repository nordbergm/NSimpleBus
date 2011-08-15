using System;
using NSimpleBus.Configuration;

namespace NSimpleBus.CompetingConsumer.Consumer
{
    class Program
    {
        static void Main(string[] args)
        {
            var bus = Bus.New(c =>
            {
                c.Broker("us1");
                c.Exchange("sample_competingconsumers");
                c.Credentials("rabbit", "rabbit");
                c.UseRabbitMq();
                c.CompetingConsumer();
                c.RegisterConsumer(() => new SimpleMessageConsumer());
            });

            Console.ReadLine();
        }
    }
}
