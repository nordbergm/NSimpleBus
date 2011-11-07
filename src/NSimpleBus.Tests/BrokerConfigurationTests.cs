using System;
using System.Linq;
using System.Reflection;
using NSimpleBus.Configuration;
using NSimpleBus.Serialization;
using NSimpleBus.Tests.Namespace1;
using NSimpleBus.Tests.Namespace2;
using NSimpleBus.Tests.Namespace3;
using Rhino.Mocks;
using Xunit;

namespace NSimpleBus.Tests
{
    public class BrokerConfigurationTests
    {
        [Fact]
        public void CanRegisterExplicitlyImplementedConsumer()
        {
            var consumer = new ExplicitTestMessageConsumer();
            var message = new TestMessage();

            BrokerConfiguration config = new BrokerConfiguration();
            config.RegisterConsumer(() => consumer);

            Assert.Equal(1, config.RegisteredConsumers.Count);
            Assert.Equal(1, config.RegisteredConsumers[typeof(TestMessage)].Count);
            Assert.Equal(typeof(TestMessage), config.RegisteredConsumers.Keys.Single());

            var addedConsumer = config.RegisteredConsumers[typeof(TestMessage)][0];
            Assert.IsType(typeof(BrokerConfiguration.RegisteredConsumer), addedConsumer);
            Assert.Equal(typeof(TestMessage), addedConsumer.MessageType);
            Assert.Equal(typeof(TestMessage).Name, addedConsumer.Queue);

            addedConsumer.Invoke(message);
        }

        [Fact]
        public void CanRegisterConsumer()
        {
            var mockRepository = new MockRepository();
            var consumer = mockRepository.StrictMock<Consumes<TestMessage>.All>();
            var message = new TestMessage();

            using (mockRepository.Record())
            {
                Expect.Call(() => consumer.Consume(message));
            }

            using (mockRepository.Playback())
            {
                BrokerConfiguration config = new BrokerConfiguration();
                config.RegisterConsumer(() => consumer);

                Assert.Equal(1, config.RegisteredConsumers.Count);
                Assert.Equal(1, config.RegisteredConsumers[typeof(TestMessage)].Count);
                Assert.Equal(typeof(TestMessage), config.RegisteredConsumers.Keys.Single());

                var addedConsumer = config.RegisteredConsumers[typeof (TestMessage)][0];
                Assert.IsType(typeof (BrokerConfiguration.RegisteredConsumer), addedConsumer);
                Assert.Equal(typeof(TestMessage), addedConsumer.MessageType);
                Assert.Equal(typeof(TestMessage).Name, addedConsumer.Queue);

                addedConsumer.Invoke(message);
            }
        }

        [Fact]
        public void CanRegisterClassWithMultipleConsumers()
        {
            var consumer = new MultipleConsumer();

            BrokerConfiguration config = new BrokerConfiguration();
            config.RegisterConsumer(() => consumer);

            Assert.Equal(2, config.RegisteredConsumers.Count);
            Assert.Equal(1, config.RegisteredConsumers[typeof(TestMessage)].Count);
            Assert.Equal(1, config.RegisteredConsumers[typeof(TestMessage2)].Count);

            var addedConsumer = config.RegisteredConsumers[typeof(TestMessage)][0];
            Assert.IsType(typeof(BrokerConfiguration.RegisteredConsumer), addedConsumer);
            Assert.Equal(typeof(TestMessage), addedConsumer.MessageType);
            Assert.Equal(typeof(TestMessage).Name, addedConsumer.Queue);

            addedConsumer = config.RegisteredConsumers[typeof(TestMessage2)][0];
            Assert.IsType(typeof(BrokerConfiguration.RegisteredConsumer), addedConsumer);
            Assert.Equal(typeof(TestMessage2), addedConsumer.MessageType);
            Assert.Equal(typeof(TestMessage2).Name, addedConsumer.Queue);
        }

        [Fact]
        public void CanRegisterConsumerFromAssembly()
        {
            BrokerConfiguration config = new BrokerConfiguration();
            config.RegisterConsumers(Assembly.GetExecutingAssembly());

            Assert.Equal(2, config.RegisteredConsumers.Count);
            Assert.Equal(4, config.RegisteredConsumers[typeof(TestMessage)].Count);
            Assert.Equal(typeof(TestMessage), config.RegisteredConsumers.Keys.ToArray()[0]);
            Assert.Equal(typeof(TestMessage2), config.RegisteredConsumers.Keys.ToArray()[1]);

            var addedConsumer = config.RegisteredConsumers[typeof(TestMessage)][0];
            Assert.IsType(typeof(BrokerConfiguration.RegisteredConsumer), addedConsumer);
            Assert.Equal(typeof(TestMessage), addedConsumer.MessageType);
            Assert.Equal(typeof(TestMessage).Name, addedConsumer.Queue);

            addedConsumer = config.RegisteredConsumers[typeof(TestMessage2)][0];
            Assert.IsType(typeof(BrokerConfiguration.RegisteredConsumer), addedConsumer);
            Assert.Equal(typeof(TestMessage2), addedConsumer.MessageType);
            Assert.Equal(typeof(TestMessage2).Name, addedConsumer.Queue);
        }

        [Fact]
        public void ConsumersRegisteredFromAssemblyCanHaveCustomResolver()
        {
            BrokerConfiguration config = new BrokerConfiguration();
            int numCalls = 0;
            config.RegisterConsumers(Assembly.GetExecutingAssembly(), null,
            t =>
            {
                numCalls++;
                return (IConsumer)Activator.CreateInstance(t);
            });

            Assert.Equal(2, config.RegisteredConsumers.Count);
            Assert.Equal(4, config.RegisteredConsumers[typeof(TestMessage)].Count);
            Assert.Equal(1, config.RegisteredConsumers[typeof(TestMessage2)].Count);

            Assert.NotEqual(0, numCalls);
        }

        [Fact]
        public void CanRegisterConsumerFromAssemblyWithNamespace()
        {
            BrokerConfiguration config = new BrokerConfiguration();
            config.RegisterConsumers(Assembly.GetExecutingAssembly(), typeof(TestConsumer).Namespace);

            Assert.Equal(1, config.RegisteredConsumers.Count);
            Assert.Equal(1, config.RegisteredConsumers[typeof(TestMessage)].Count);
            Assert.Equal(typeof(TestMessage), config.RegisteredConsumers.Keys.Single());

            var addedConsumer = config.RegisteredConsumers[typeof(TestMessage)][0];
            Assert.IsType(typeof(BrokerConfiguration.RegisteredConsumer), addedConsumer);
            Assert.Equal(typeof(TestMessage), addedConsumer.MessageType);
            Assert.Equal(typeof(TestMessage).Name, addedConsumer.Queue);
        }

        [Fact]
        public void CanRegisterSubscriber()
        {
            var mockRepository = new MockRepository();
            var subscriber = mockRepository.StrictMock<Subscribes<TestMessage>.All>();
            var message = new TestMessage();

            using (mockRepository.Record())
            {
                Expect.Call(() => subscriber.Consume(message));
            }

            using (mockRepository.Playback())
            {
                BrokerConfiguration config = new BrokerConfiguration();
                config.RegisterSubscriber(() => subscriber);

                Assert.Equal(1, config.RegisteredConsumers.Count);
                Assert.Equal(1, config.RegisteredConsumers[typeof(TestMessage)].Count);
                Assert.Equal(typeof(TestMessage), config.RegisteredConsumers.Keys.Single());

                var addedSubscriber = config.RegisteredConsumers[typeof(TestMessage)][0];
                Assert.IsType(typeof(BrokerConfiguration.RegisteredSubscriber), addedSubscriber);
                Assert.Equal(typeof(TestMessage), addedSubscriber.MessageType);
                Assert.Equal(typeof(TestMessage).Name, addedSubscriber.Queue);
                Assert.True(addedSubscriber.AutoDeleteQueue);

                addedSubscriber.Invoke(message);
            }
        }

        [Fact]
        public void SubscriberNotAutoDeleteWhenQueueNameResolverDefined()
        {
            var mockRepository = new MockRepository();
            var subscriber = mockRepository.StrictMock<Subscribes<TestMessage>.All>();
            var message = new TestMessage();

            using (mockRepository.Record())
            {
                Expect.Call(() => subscriber.Consume(message));
            }

            using (mockRepository.Playback())
            {
                BrokerConfiguration config = new BrokerConfiguration();
                config.ResolveQueueName = (t, c) => "myqueue";
                config.RegisterSubscriber(() => subscriber);

                Assert.Equal(1, config.RegisteredConsumers.Count);

                var addedSubscriber = config.RegisteredConsumers[typeof(TestMessage)][0];
                Assert.False(addedSubscriber.AutoDeleteQueue);
                Assert.Equal("myqueue", addedSubscriber.Queue);

                addedSubscriber.Invoke(message);
            }
        }

        [Fact]
        public void CanRegisterSubscribersFromAssembly()
        {
            BrokerConfiguration config = new BrokerConfiguration();
            config.RegisterSubscribers(Assembly.GetExecutingAssembly());

            Assert.Equal(1, config.RegisteredConsumers.Count);
            Assert.Equal(2, config.RegisteredConsumers[typeof(TestMessage)].Count);
            Assert.Equal(typeof(TestMessage), config.RegisteredConsumers.Keys.Single());

            var addedSubscriber = config.RegisteredConsumers[typeof(TestMessage)][0];
            Assert.IsType(typeof(BrokerConfiguration.RegisteredSubscriber), addedSubscriber);
            Assert.Equal(typeof(TestMessage), addedSubscriber.MessageType);
            Assert.Equal(typeof(TestMessage).Name, addedSubscriber.Queue);

            addedSubscriber = config.RegisteredConsumers[typeof(TestMessage)][1];
            Assert.IsType(typeof(BrokerConfiguration.RegisteredSubscriber), addedSubscriber);
            Assert.Equal(typeof(TestMessage), addedSubscriber.MessageType);
            Assert.Equal(typeof(TestMessage).Name, addedSubscriber.Queue);
        }

        [Fact]
        public void SubscribersRegisteredFromAssemblyCanHaveCustomResolver()
        {
            BrokerConfiguration config = new BrokerConfiguration();
            int numCalls = 0;
            config.RegisterSubscribers(Assembly.GetExecutingAssembly(), null,
            t =>
            {
                numCalls++;
                return (ISubscriber)Activator.CreateInstance(t);
            });

            Assert.Equal(1, config.RegisteredConsumers.Count);
            Assert.Equal(2, config.RegisteredConsumers[typeof(TestMessage)].Count);

            Assert.NotEqual(0, numCalls);
        }

        [Fact]
        public void CanRegisterSubscribersFromAssemblyWithNamespace()
        {
            BrokerConfiguration config = new BrokerConfiguration();
            config.RegisterSubscribers(Assembly.GetExecutingAssembly(), typeof(TestSubscriber).Namespace);

            Assert.Equal(1, config.RegisteredConsumers.Count);
            Assert.Equal(1, config.RegisteredConsumers[typeof(TestMessage)].Count);
            Assert.Equal(typeof(TestMessage), config.RegisteredConsumers.Keys.Single());

            var addedSubscriber = config.RegisteredConsumers[typeof(TestMessage)][0];
            Assert.IsType(typeof(BrokerConfiguration.RegisteredSubscriber), addedSubscriber);
            Assert.Equal(typeof(TestMessage), addedSubscriber.MessageType);
            Assert.Equal(typeof(TestMessage).Name, addedSubscriber.Queue);
        }

        [Fact]
        public void CanSetCredentials()
        {
            var mockRepository = new MockRepository();
            var configMock = mockRepository.StrictMock<IBrokerConfiguration>();

            using (mockRepository.Record())
            {
                Expect.Call(() => configMock.UserName = "username");
                Expect.Call(() => configMock.Password = "password");
            }

            using (mockRepository.Playback())
            {
                configMock.Credentials("username", "password");
            }
        }

        [Fact]
        public void CanSetJsonSerializer()
        {
            var mockRepository = new MockRepository();
            var configMock = mockRepository.StrictMock<IBrokerConfiguration>();

            using (mockRepository.Record())
            {
                Expect.Call(() => configMock.Serializer = null)
                    .IgnoreArguments()
                    .WhenCalled(mi => Assert.IsType(typeof(JsonSerializer), mi.Arguments[0]));
            }

            using (mockRepository.Playback())
            {
                configMock.JsonSerializer();
            }
        }

        [Fact]
        public void CanSetBroker()
        {
            var mockRepository = new MockRepository();
            var configMock = mockRepository.StrictMock<IBrokerConfiguration>();

            using (mockRepository.Record())
            {
                Expect.Call(() => configMock.HostName = "hostname");
                Expect.Call(() => configMock.Port = 1234);
            }

            using (mockRepository.Playback())
            {
                configMock.Broker("hostname", 1234);
            }
        }

        [Fact]
        public void CanSetExchange()
        {
            var mockRepository = new MockRepository();
            var configMock = mockRepository.StrictMock<IBrokerConfiguration>();

            using (mockRepository.Record())
            {
                Expect.Call(() => configMock.Exchange = "exchange");
            }

            using (mockRepository.Playback())
            {
                configMock.Exchange("exchange");
            }
        }

        [Fact]
        public void CanSetCompetingConsumer()
        {
            var mockRepository = new MockRepository();
            var configMock = mockRepository.StrictMock<IBrokerConfiguration>();

            using (mockRepository.Record())
            {
                Expect.Call(() => configMock.AutoConfigure = AutoConfigureMode.CompetingConsumer);
            }

            using (mockRepository.Playback())
            {
                configMock.CompetingConsumer();
            }
        }

        [Fact]
        public void CanPublishSubscribe()
        {
            var mockRepository = new MockRepository();
            var configMock = mockRepository.StrictMock<IBrokerConfiguration>();

            using (mockRepository.Record())
            {
                Expect.Call(() => configMock.AutoConfigure = AutoConfigureMode.PublishSubscribe);
            }

            using (mockRepository.Playback())
            {
                configMock.PublishSubscribe();
            }
        }
    }
}
