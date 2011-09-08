using System.Linq;
using NSimpleBus.Configuration;
using NSimpleBus.Transports.RabbitMQ.Serialization;
using RabbitMQ.Client;
using Rhino.Mocks;
using Xunit;
using NSimpleBus.Tests;
using System.Threading;
using System.Security.Principal;

namespace NSimpleBus.Transports.RabbitMQ.Tests
{
    public class GroupedCallbackConsumerTests
    {
        [Fact]
        public void IsRunningAtCreation()
        {
            var mockRepository = new MockRepository();
            var rabbitModel = mockRepository.DynamicMock<IModel>();
            var messageSerializer = mockRepository.DynamicMock<IMessageSerializer>();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();

            var callbackConsumer = new GroupedCallbackConsumer(rabbitModel, messageSerializer, config);
            Assert.True(callbackConsumer.IsRunning);

            callbackConsumer.Close();
            callbackConsumer.Dispose();
        }

        [Fact]
        public void IsNotRunningAfterClose()
        {
            var mockRepository = new MockRepository();
            var rabbitModel = mockRepository.DynamicMock<IModel>();
            var messageSerializer = mockRepository.DynamicMock<IMessageSerializer>();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();

            var callbackConsumer = new GroupedCallbackConsumer(rabbitModel, messageSerializer, config);
            callbackConsumer.Close();

            Assert.False(callbackConsumer.IsRunning);
        }

        [Fact]
        public void IsNotRunningAfterDispose()
        {
            var mockRepository = new MockRepository();
            var rabbitModel = mockRepository.DynamicMock<IModel>();
            var messageSerializer = mockRepository.DynamicMock<IMessageSerializer>();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();

            var callbackConsumer = new GroupedCallbackConsumer(rabbitModel, messageSerializer, config);
            callbackConsumer.Dispose();

            Assert.False(callbackConsumer.IsRunning);
        }

        [Fact]
        public void ClosesRegisteredConsumerTokensOnClose()
        {
            var mockRepository = new MockRepository();
            var rabbitModel = mockRepository.DynamicMock<IModel>();
            var messageSerializer = mockRepository.DynamicMock<IMessageSerializer>();
            var registeredConsumer = mockRepository.DynamicMock<IRegisteredConsumer>();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();

            using (mockRepository.Record())
            {
                SetupResult.For(registeredConsumer.Queue).Return("q");
            }

            using (mockRepository.Playback())
            {
                var callbackConsumer = new GroupedCallbackConsumer(rabbitModel, messageSerializer, config);
                callbackConsumer.ConsumeQueue(registeredConsumer);

                GroupedCallbackConsumer.QueueConsumer queueConsumer = callbackConsumer.QueueConsumers.Single().Value;

                Assert.False(queueConsumer.ConsumeToken.IsClosed);

                callbackConsumer.Close();

                Assert.True(queueConsumer.ConsumeToken.IsClosed);
            }
        }

        [Fact]
        public void CanRegisterConsumer()
        {
            var mockRepository = new MockRepository();
            var rabbitModel = mockRepository.DynamicMock<IModel>();
            var messageSerializer = mockRepository.DynamicMock<IMessageSerializer>();
            var registeredConsumer = mockRepository.DynamicMock<IRegisteredConsumer>();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();

            using (mockRepository.Record())
            {
                SetupResult.For(registeredConsumer.Queue).Return("q");

                Expect.Call(rabbitModel.BasicConsume(null, false, null))
                    .IgnoreArguments()
                    .WhenCalled(mi =>
                                    {
                                        Assert.Equal("q", mi.Arguments[0] as string);
                                        Assert.Equal(false, (bool)mi.Arguments[1]);
                                        Assert.IsType(typeof(QueueActivityConsumer), mi.Arguments[2]);
                                    }).Return("token");
            }

            using (mockRepository.Playback())
            {
                var callbackConsumer = new GroupedCallbackConsumer(rabbitModel, messageSerializer, config);
                callbackConsumer.ConsumeQueue(registeredConsumer);

                Assert.Equal(1, callbackConsumer.QueueConsumers.Count);
                Assert.Equal(1, callbackConsumer.QueueConsumers.Values.First().RegisteredConsumers.Count);
                Assert.Equal("token", callbackConsumer.QueueConsumers.Values.First().ConsumeToken.ConsumerTag);

                callbackConsumer.Close();
                callbackConsumer.Dispose();
            }
        }

        [Fact]
        public void AcksOnDelivery()
        {
            var mockRepository = new MockRepository();
            var rabbitModel = mockRepository.DynamicMock<IModel>();
            var messageSerializer = mockRepository.DynamicMock<IMessageSerializer>();
            var registeredConsumer = mockRepository.DynamicMock<IRegisteredConsumer>();
            var properties = mockRepository.Stub<IBasicProperties>();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();

            using (mockRepository.Record())
            {
                SetupResult.For(registeredConsumer.MessageType).Return(typeof (TestMessage));
                SetupResult.For(registeredConsumer.Queue).Return("q");
                SetupResult.For(messageSerializer.DeserializeMessage(null)).IgnoreArguments().Return(
                    mockRepository.Stub<IMessageEnvelope<TestMessage>>());
                Expect.Call(() => rabbitModel.BasicAck(1, false));
            }

            using (mockRepository.Playback())
            {
                var callbackConsumer = new GroupedCallbackConsumer(rabbitModel, messageSerializer, config);
                callbackConsumer.ConsumeQueue(registeredConsumer);
                callbackConsumer.QueueConsumers["q"].Consumer.HandleBasicDeliver("ct1", 1, false, "ex", typeof(TestMessage).ToRoutingKey(), properties, new byte[0]);

                callbackConsumer.Close();
                callbackConsumer.Dispose();
            }
        }

        [Fact]
        public void DeserializesOnDelivery()
        {
            var mockRepository = new MockRepository();
            var rabbitModel = mockRepository.DynamicMock<IModel>();
            var messageSerializer = mockRepository.DynamicMock<IMessageSerializer>();
            var registeredConsumer = mockRepository.DynamicMock<IRegisteredConsumer>();
            var properties = mockRepository.Stub<IBasicProperties>();
            var envelope = mockRepository.DynamicMock<IMessageEnvelope<TestMessage>>();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();

            using (mockRepository.Record())
            {
                SetupResult.For(registeredConsumer.MessageType).Return(typeof(TestMessage));
                SetupResult.For(registeredConsumer.Queue).Return("q");
                Expect.Call(messageSerializer.DeserializeMessage(null))
                    .IgnoreArguments()
                    .WhenCalled(mi =>

                        {
                            var args = mi.Arguments[0] as QueueActivityConsumer.DeliverEventArgs;

                            Assert.Equal(new byte[0], args.Body);
                            Assert.Equal("ct1", args.ConsumerTag);
                            Assert.Equal(1, (long)args.DeliveryTag);
                            Assert.Equal("ex", args.Exchange);
                            Assert.Same(properties, args.Properties);
                            Assert.Equal("q", args.Queue);
                            Assert.Equal(false, args.Redelivered);
                            Assert.Equal(typeof(TestMessage).ToRoutingKey(), args.RoutingKey);
                        })
                    .Return(envelope);
            }

            using (mockRepository.Playback())
            {
                var callbackConsumer = new GroupedCallbackConsumer(rabbitModel, messageSerializer, config);
                callbackConsumer.ConsumeQueue(registeredConsumer);
                callbackConsumer.QueueConsumers["q"].Consumer.HandleBasicDeliver("ct1", 1, false, "ex", typeof(TestMessage).ToRoutingKey(), properties, new byte[0]);

                callbackConsumer.Close();
                callbackConsumer.Dispose();
            }
        }

        [Fact]
        public void InvokesOnDelivery()
        {
            var mockRepository = new MockRepository();
            var rabbitModel = mockRepository.DynamicMock<IModel>();
            var messageSerializer = mockRepository.DynamicMock<IMessageSerializer>();
            var registeredConsumer = mockRepository.DynamicMock<IRegisteredConsumer>();
            var properties = mockRepository.Stub<IBasicProperties>();
            var envelope = mockRepository.DynamicMock<IMessageEnvelope<TestMessage>>();
            var message = new TestMessage();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();

            using (mockRepository.Record())
            {
                SetupResult.For(registeredConsumer.MessageType).Return(typeof(TestMessage));
                SetupResult.For(registeredConsumer.Queue).Return("q");
                SetupResult.For(envelope.Message).Return(message);
                SetupResult.For(messageSerializer.DeserializeMessage(null)).IgnoreArguments().Return(envelope);

                Expect.Call(() => registeredConsumer.Invoke(message));
            }

            using (mockRepository.Playback())
            {
                var callbackConsumer = new GroupedCallbackConsumer(rabbitModel, messageSerializer, config);
                callbackConsumer.ConsumeQueue(registeredConsumer);
                callbackConsumer.QueueConsumers["q"].Consumer.HandleBasicDeliver("ct1", 1, false, "ex", typeof(TestMessage).ToRoutingKey(), properties, new byte[0]);

                callbackConsumer.Close();
                callbackConsumer.Dispose();
            }
        }

        [Fact]
        public void SetsPrincipalOnDelivery()
        {
            var mockRepository = new MockRepository();
            var rabbitModel = mockRepository.DynamicMock<IModel>();
            var messageSerializer = mockRepository.DynamicMock<IMessageSerializer>();
            var registeredConsumer = mockRepository.DynamicMock<IRegisteredConsumer>();
            var properties = mockRepository.Stub<IBasicProperties>();
            var envelope = mockRepository.DynamicMock<IMessageEnvelope<TestMessage>>();
            var message = new TestMessage();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();

            using (mockRepository.Record())
            {
                SetupResult.For(envelope.UserName).Return("user1");
                SetupResult.For(registeredConsumer.MessageType).Return(typeof(TestMessage));
                SetupResult.For(registeredConsumer.Queue).Return("q");
                SetupResult.For(envelope.Message).Return(message);
                SetupResult.For(messageSerializer.DeserializeMessage(null)).IgnoreArguments().Return(envelope);
                SetupResult.For(config.CreatePrincipal).Return(
                    n => new GenericPrincipal(new GenericIdentity(n + "set"), new string[0]));

                Expect.Call(() => registeredConsumer.Invoke(message)).WhenCalled(mi => Assert.Equal("user1set", Thread.CurrentPrincipal.Identity.Name));
            }

            using (mockRepository.Playback())
            {
                var callbackConsumer = new GroupedCallbackConsumer(rabbitModel, messageSerializer, config);
                callbackConsumer.ConsumeQueue(registeredConsumer);
                callbackConsumer.QueueConsumers["q"].Consumer.HandleBasicDeliver("ct1", 1, false, "ex", typeof(TestMessage).ToRoutingKey(), properties, new byte[0]);

                callbackConsumer.Close();
                callbackConsumer.Dispose();
            }
        }
    }
}
