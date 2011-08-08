using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSimpleBus.Configuration;
using NSimpleBus.Transports;
using Rhino.Mocks;
using Xunit;

namespace NSimpleBus.Tests
{
    public class BusTests
    {
        [Fact]
        public void CtorRegistersConsumers()
        {
            var mockRepository = new MockRepository();
            var config = mockRepository.StrictMock<IBrokerConfiguration>();
            var connFactory = mockRepository.StrictMock<IBrokerConnectionFactory>();
            var conn = mockRepository.StrictMock<IBrokerConnection>();
            var consumer = mockRepository.StrictMock<IRegisteredConsumer>();

            using (mockRepository.Record())
            {
                SetupResult.For(connFactory.CreateConnection()).Return(conn);
                SetupResult.For(config.ConnectionFactory).Return(connFactory);
                SetupResult.For(config.RegisteredConsumers).Return(new Dictionary<Type, IRegisteredConsumer> { { typeof(TestMessage), consumer } });

                Expect.Call(() => conn.Consume(consumer));
            }

            using (mockRepository.Playback())
            {
                new Bus(config);
            }
        }

        [Fact]
        public void ClosesOpenConnectionOnDispose()
        {
            var mockRepository = new MockRepository();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();
            var connFactory = mockRepository.DynamicMock<IBrokerConnectionFactory>();
            var conn = mockRepository.DynamicMock<IBrokerConnection>();
            var consumer = mockRepository.DynamicMock<IRegisteredConsumer>();
            
            using (mockRepository.Record())
            {
                SetupResult.For(connFactory.CreateConnection()).Return(conn);
                SetupResult.For(config.ConnectionFactory).Return(connFactory);
                SetupResult.For(config.RegisteredConsumers).Return(new Dictionary<Type, IRegisteredConsumer> { { typeof(TestMessage), consumer } });
                SetupResult.For(conn.IsOpen).Return(true);

                Expect.Call(conn.Close);
            }

            using (mockRepository.Playback())
            {
                var bus = new Bus(config);
                bus.Dispose();
            }
        }

        [Fact]
        public void CanPublish()
        {
            var mockRepository = new MockRepository();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();
            var connFactory = mockRepository.DynamicMock<IBrokerConnectionFactory>();
            var conn = mockRepository.DynamicMock<IBrokerConnection>();
            var consumer = mockRepository.DynamicMock<IRegisteredConsumer>();
            var message = new TestMessage();

            using (mockRepository.Record())
            {
                SetupResult.For(connFactory.CreateConnection()).Return(conn);
                SetupResult.For(config.Exchange).Return("ex");
                SetupResult.For(config.ConnectionFactory).Return(connFactory);
                SetupResult.For(config.RegisteredConsumers).Return(new Dictionary<Type, IRegisteredConsumer> { { typeof(TestMessage), consumer } });
                SetupResult.For(conn.IsOpen).Return(true);

                Expect.Call(() => conn.Publish<TestMessage>(null, null))
                    .IgnoreArguments()
                    .WhenCalled(mi =>
                            {
                                var envelope = mi.Arguments[0];
                                var exchange = mi.Arguments[1];

                                Assert.IsAssignableFrom(typeof(IMessageEnvelope<TestMessage>), envelope);
                                Assert.Same(message, ((IMessageEnvelope<TestMessage>)envelope).Message);
                                Assert.Equal("ex", exchange);
                            });
            }

            using (mockRepository.Playback())
            {
                var bus = new Bus(config);
                bus.Publish(message);
            }
        }

        [Fact]
        public void ReusesConnectionWhenPublishing()
        {
            var mockRepository = new MockRepository();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();
            var connFactory = mockRepository.DynamicMock<IBrokerConnectionFactory>();
            var conn = mockRepository.DynamicMock<IBrokerConnection>();
            var consumer = mockRepository.DynamicMock<IRegisteredConsumer>();
            var message = new TestMessage();

            using (mockRepository.Record())
            {
                SetupResult.For(config.Exchange).Return("ex");
                SetupResult.For(config.ConnectionFactory).Return(connFactory);
                SetupResult.For(config.RegisteredConsumers).Return(new Dictionary<Type, IRegisteredConsumer> { { typeof(TestMessage), consumer } });
                SetupResult.For(conn.IsOpen).Return(true);

                Expect.Call(connFactory.CreateConnection()).Repeat.Once().Return(conn);
            }

            using (mockRepository.Playback())
            {
                var bus = new Bus(config);
                bus.Publish(message);
                bus.Publish(message);
            }
        }
    }
}
