﻿using System;
using System.Threading;
using NSimpleBus.Configuration;
using NSimpleBus.PubSub.Common;

namespace NSimpleBus.PubSub.Publisher
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
                                  });

            Thread.Sleep(5000);

            int i;
            for (i = 0; i < 100; i++)
            {
                var message = new SimpleMessage { Created = DateTime.Now, Id = i };
                bus.Publish(message);
                Console.WriteLine("Published message {0} created {1}", message.Id, message.Created);
            }

            Console.WriteLine("Now send a message by hitting RETURN.");

            while (Console.ReadLine() != null)
            {
                var message = new SimpleMessage { Created = DateTime.Now, Id = i++ };
                bus.Publish(message);
                Console.WriteLine("Published message {0} created {1}", message.Id, message.Created);
            }
        }
    }
}
