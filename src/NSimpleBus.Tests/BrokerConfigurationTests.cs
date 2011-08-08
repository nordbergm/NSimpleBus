using System.Linq;
using NSimpleBus.Configuration;
using NSimpleBus.Serialization;
using Rhino.Mocks;
using Xunit;

namespace NSimpleBus.Tests
{
    public class BrokerConfigurationTests
    {
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
                config.RegisterConsumer(consumer);

                Assert.Equal(1, config.RegisteredConsumers.Count);
                Assert.Equal(typeof(TestMessage), config.RegisteredConsumers.Keys.Single());

                var addedConsumer = config.RegisteredConsumers[typeof (TestMessage)];
                Assert.IsType(typeof (BrokerConfiguration.RegisteredConsumer), addedConsumer);
                Assert.Equal(typeof(TestMessage), addedConsumer.MessageType);
                Assert.Equal(typeof(TestMessage).FullName, addedConsumer.Queue);
                Assert.Same(consumer, addedConsumer.Consumer);

                addedConsumer.Invoke(message);
            }
        }

        [Fact]
        public void CanRegisterSubscriber()
        {
            var mockRepository = new MockRepository();
            var subscriber = mockRepository.StrictMock<Consumes<TestMessage>.All>();
            var message = new TestMessage();

            using (mockRepository.Record())
            {
                Expect.Call(() => subscriber.Consume(message));
            }

            using (mockRepository.Playback())
            {
                BrokerConfiguration config = new BrokerConfiguration();
                config.RegisterSubscriber(subscriber);

                Assert.Equal(1, config.RegisteredConsumers.Count);
                Assert.Equal(typeof(TestMessage), config.RegisteredConsumers.Keys.Single());

                var addedSubscriber = config.RegisteredConsumers[typeof(TestMessage)];
                Assert.IsType(typeof(BrokerConfiguration.RegisteredPubSubConsumer), addedSubscriber);
                Assert.Equal(typeof(TestMessage), addedSubscriber.MessageType);
                Assert.NotEqual(typeof(TestMessage).FullName, addedSubscriber.Queue);
                Assert.Contains(typeof(TestMessage).FullName, addedSubscriber.Queue);
                Assert.Same(subscriber, addedSubscriber.Consumer);

                addedSubscriber.Invoke(message);
            }
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
