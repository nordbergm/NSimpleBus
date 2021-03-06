﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using NSimpleBus.Serialization;
using NSimpleBus.Tests;
using NSimpleBus.Tests.Namespace1;
using NSimpleBus.Transports.RabbitMQ.Serialization;
using RabbitMQ.Client;
using Xunit;
using Rhino.Mocks;
using NSimpleBus.Configuration;

namespace NSimpleBus.Transports.RabbitMQ.Tests
{
    public class BrokerConnectionTests
    {
        [Fact]
        public void AutoConfiguresExchangeWhenPubSubAutoConfig()
        {
            MockRepository mockRepository = new MockRepository();

            var rabbitConn = mockRepository.DynamicMock<IConnection>();
            var rabbitModel = mockRepository.DynamicMock<IModel>();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();
            var serializer = mockRepository.DynamicMock<IMessageSerializer>();
            var callbackConsumer = mockRepository.DynamicMock<ICallbackConsumer>();
            var consumer = mockRepository.DynamicMock<IRegisteredConsumer>();

            using (mockRepository.Record())
            {
                SetupResult.For(rabbitConn.CreateModel()).Return(rabbitModel);
                
                SetupResult.For(config.Exchange).Return("ex");
                SetupResult.For(config.AutoConfigure).Return(AutoConfigureMode.PublishSubscribe);

                SetupResult.For(consumer.Queue).Return("q");
                SetupResult.For(consumer.MessageType).Return(typeof(TestMessage));
                SetupResult.For(consumer.ConsumerType).Return(typeof(TestConsumer));

                Expect.Call(() => rabbitModel.ExchangeDeclare(string.Concat("ex.", typeof(TestMessage).FullName), "fanout", true, false, null));
            }

            using (mockRepository.Playback())
            {
                new BrokerConnection(rabbitConn, rabbitModel, config, serializer, callbackConsumer).Consume(consumer);
            }
        }

        [Fact]
        public void AutoConfiguresExchangeWhenCompetingConsumerAutoConfig()
        {
            MockRepository mockRepository = new MockRepository();

            var rabbitConn = mockRepository.DynamicMock<IConnection>();
            var rabbitModel = mockRepository.DynamicMock<IModel>();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();
            var serializer = mockRepository.DynamicMock<IMessageSerializer>();
            var callbackConsumer = mockRepository.DynamicMock<ICallbackConsumer>();
            var consumer = mockRepository.DynamicMock<IRegisteredConsumer>();

            using (mockRepository.Record())
            {
                SetupResult.For(rabbitConn.CreateModel()).Return(rabbitModel);
                
                SetupResult.For(config.Exchange).Return("ex");
                SetupResult.For(config.AutoConfigure).Return(AutoConfigureMode.CompetingConsumer);

                SetupResult.For(consumer.Queue).Return("q");
                SetupResult.For(consumer.MessageType).Return(typeof(TestMessage));
                SetupResult.For(consumer.ConsumerType).Return(typeof(TestConsumer));
                
                Expect.Call(() => rabbitModel.ExchangeDeclare("ex", "direct", true, false, null));
            }

            using (mockRepository.Playback())
            {
                new BrokerConnection(rabbitConn, rabbitModel, config, serializer, callbackConsumer).Consume(consumer);
            }
        }

        [Fact]
        public void DoesNotAutoConfigureExchangeWhenAutoConfigNone()
        {
            MockRepository mockRepository = new MockRepository();

            var rabbitConn = mockRepository.DynamicMock<IConnection>();
            var rabbitModel = mockRepository.DynamicMock<IModel>();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();
            var serializer = mockRepository.DynamicMock<IMessageSerializer>();
            var callbackConsumer = mockRepository.DynamicMock<ICallbackConsumer>();

            using (mockRepository.Record())
            {
                SetupResult.For(rabbitConn.CreateModel()).Return(rabbitModel);
                SetupResult.For(config.Exchange).Return("ex");
                SetupResult.For(config.AutoConfigure).Return(AutoConfigureMode.None);

                Expect.Call(() => rabbitModel.ExchangeDeclare(null, null)).IgnoreArguments().Repeat.Never();
                Expect.Call(() => rabbitModel.ExchangeDeclare(null, null, false)).IgnoreArguments().Repeat.Never();
                Expect.Call(() => rabbitModel.ExchangeDeclare(null, null, false, false, null)).IgnoreArguments().Repeat.Never();
            }

            using (mockRepository.Playback())
            {
                new BrokerConnection(rabbitConn, rabbitModel, config, serializer, callbackConsumer);
            }
        }

        [Fact]
        public void AutoConfiguresQueueWhenPubSubAutoConfig()
        {
            MockRepository mockRepository = new MockRepository();

            var rabbitConn = mockRepository.DynamicMock<IConnection>();
            var rabbitModel = mockRepository.DynamicMock<IModel>();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();
            var consumer = mockRepository.DynamicMock<IRegisteredConsumer>();
            var serializer = mockRepository.DynamicMock<IMessageSerializer>();
            var callbackConsumer = mockRepository.DynamicMock<ICallbackConsumer>();

            using (mockRepository.Record())
            {
                SetupResult.For(consumer.Queue).Return("q");
                SetupResult.For(consumer.MessageType).Return(typeof(TestMessage));
                SetupResult.For(consumer.ConsumerType).Return(typeof(TestConsumer));
                SetupResult.For(consumer.AutoDeleteQueue).Return(true);

                SetupResult.For(rabbitConn.CreateModel()).Return(rabbitModel);
                SetupResult.For(config.Exchange).Return("ex");
                SetupResult.For(config.AutoConfigure).Return(AutoConfigureMode.PublishSubscribe);

                Expect.Call(rabbitModel.QueueDeclare("PublishSubscribe." + typeof(TestConsumer).FullName + ".q", true, false, true, null)).Return(new QueueDeclareOk("PublishSubscribe.q", 0, 0));
            }

            using (mockRepository.Playback())
            {
                new BrokerConnection(rabbitConn, rabbitModel, config, serializer, callbackConsumer).Consume(consumer);
            }
        }
      
        [Fact]
        public void AutoConfiguresQueueWhenCompetingConsumerAutoConfig()
        {
            MockRepository mockRepository = new MockRepository();

            var rabbitConn = mockRepository.DynamicMock<IConnection>();
            var rabbitModel = mockRepository.DynamicMock<IModel>();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();
            var consumer = mockRepository.DynamicMock<IRegisteredConsumer>();
            var serializer = mockRepository.DynamicMock<IMessageSerializer>();
            var callbackConsumer = mockRepository.DynamicMock<ICallbackConsumer>();

            using (mockRepository.Record())
            {
                SetupResult.For(consumer.Queue).Return("q");
                SetupResult.For(consumer.MessageType).Return(typeof(TestMessage));
                SetupResult.For(consumer.ConsumerType).Return(typeof(TestConsumer));

                SetupResult.For(rabbitConn.CreateModel()).Return(rabbitModel);
                SetupResult.For(config.Exchange).Return("ex");
                SetupResult.For(config.AutoConfigure).Return(AutoConfigureMode.CompetingConsumer);

                Expect.Call(rabbitModel.QueueDeclare("CompetingConsumer." + typeof(TestConsumer).FullName + ".q", true, false, false, null)).Return(new QueueDeclareOk("CompetingConsumer.q", 0, 0));
            }

            using (mockRepository.Playback())
            {
                new BrokerConnection(rabbitConn, rabbitModel, config, serializer, callbackConsumer).Consume(consumer);
            }
        }

        [Fact]
        public void DoesNotAutoConfigureQueueWhenAutoConfigNone()
        {
            MockRepository mockRepository = new MockRepository();

            var rabbitConn = mockRepository.DynamicMock<IConnection>();
            var rabbitModel = mockRepository.DynamicMock<IModel>();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();
            var consumer = mockRepository.DynamicMock<IRegisteredConsumer>();
            var serializer = mockRepository.DynamicMock<IMessageSerializer>();
            var callbackConsumer = mockRepository.DynamicMock<ICallbackConsumer>();

            using (mockRepository.Record())
            {
                SetupResult.For(consumer.Queue).Return("q");
                SetupResult.For(consumer.MessageType).Return(typeof(TestMessage));
                SetupResult.For(consumer.ConsumerType).Return(typeof(TestConsumer));

                SetupResult.For(rabbitConn.CreateModel()).Return(rabbitModel);
                SetupResult.For(config.Exchange).Return("ex");
                SetupResult.For(config.AutoConfigure).Return(AutoConfigureMode.None);

                Expect.Call(rabbitModel.QueueDeclare()).IgnoreArguments().Repeat.Never();
                Expect.Call(rabbitModel.QueueDeclare("q", false, false, false, null)).IgnoreArguments().Repeat.Never();
            }

            using (mockRepository.Playback())
            {
                new BrokerConnection(rabbitConn, rabbitModel, config, serializer, callbackConsumer).Consume(consumer);
            }
        }

        [Fact]
        public void CanPublish()
        {
            var mockRepository = new MockRepository();
            var envelope = mockRepository.DynamicMock<IMessageEnvelope<TestMessage>>();
            var rabbitConn = mockRepository.DynamicMock<IConnection>();
            var rabbitModel = mockRepository.StrictMock<IModel>();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();
            var messageSerializer = mockRepository.DynamicMock<IMessageSerializer>();
            var callbackConsumer = mockRepository.DynamicMock<ICallbackConsumer>();
            var basicProperties = mockRepository.Stub<IBasicProperties>();
            var pipelineEvents = new PipelineEvents();
            pipelineEvents.MessageSending += (sender, args) => args.MessageEnvelope.Headers.Add("a", "b");

            using (mockRepository.Record())
            {
                SetupResult.For(config.Exchange).Return("ex");
                SetupResult.For(rabbitConn.CreateModel()).Return(rabbitModel);
                SetupResult.For(config.PipelineEvents).Return(pipelineEvents);
                SetupResult.For(envelope.Headers).Return(new Dictionary<string, string>());

                var stream = new JsonSerializer().Serialize(envelope);
                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);

                IBasicProperties headers;
                byte[] body;
                string routingKey;
                Expect.Call(() => messageSerializer.SerializeMessage(envelope, rabbitModel, out headers, out body, out routingKey))
                    .WhenCalled(mi =>
                                    {
                                        Assert.Equal("b", ((IMessageEnvelope<object>)mi.Arguments[0]).Headers["a"]);
                                        mi.Arguments[2] = basicProperties;
                                        mi.Arguments[3] = data;
                                        mi.Arguments[4] = "routing";
                                    });
                Expect.Call(() => rabbitModel.BasicPublish("ex", "routing", basicProperties, data));
            }

            using (mockRepository.Playback())
            {
                new BrokerConnection(rabbitConn, rabbitModel, config, messageSerializer, callbackConsumer).Publish(envelope);
            }
        }

        [Fact]
        public void ClosesOpenConnectionOnDispose()
        {
            var mockRepository = new MockRepository();
            var rabbitConn = mockRepository.DynamicMock<IConnection>();
            var rabbitModel = mockRepository.DynamicMock<IModel>();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();
            var serializer = mockRepository.DynamicMock<IMessageSerializer>();
            var callbackConsumer = mockRepository.DynamicMock<ICallbackConsumer>();

            using (mockRepository.Record())
            {
                SetupResult.For(rabbitConn.IsOpen).Return(true);
                SetupResult.For(rabbitConn.CreateModel()).Return(rabbitModel);

                Expect.Call(() => rabbitConn.Close(200, "Goodbye"));
            }

            using (mockRepository.Playback())
            {
                new BrokerConnection(rabbitConn, rabbitModel, config, serializer, callbackConsumer).Dispose();
            }
        }

        [Fact]
        public void ClosesOpenModelOnClose()
        {
            var mockRepository = new MockRepository();
            var rabbitConn = mockRepository.DynamicMock<IConnection>();
            var rabbitModel = mockRepository.DynamicMock<IModel>();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();
            var serializer = mockRepository.DynamicMock<IMessageSerializer>();
            var callbackConsumer = mockRepository.DynamicMock<ICallbackConsumer>();

            using (mockRepository.Record())
            {
                SetupResult.For(rabbitConn.IsOpen).Return(true);
                SetupResult.For(rabbitConn.CreateModel()).Return(rabbitModel);
                SetupResult.For(rabbitModel.IsOpen).Return(true);
                SetupResult.For(callbackConsumer.IsRunning).Return(true);

                Expect.Call(callbackConsumer.Close);
                Expect.Call(callbackConsumer.Dispose);
                Expect.Call(() => rabbitModel.Close(200, "Goodbye"));
                Expect.Call(rabbitModel.Dispose);
                Expect.Call(() => rabbitConn.Close(200, "Goodbye"));
                Expect.Call(rabbitConn.Dispose);
            }

            using (mockRepository.Playback())
            {
                new BrokerConnection(rabbitConn, rabbitModel, config, serializer, callbackConsumer).Close();
            }
        }
    }
}
