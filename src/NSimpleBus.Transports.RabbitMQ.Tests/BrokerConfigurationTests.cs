using NSimpleBus.Configuration;
using NSimpleBus.Serialization;
using RabbitMQ.Client;
using Rhino.Mocks;
using Xunit;

namespace NSimpleBus.Transports.RabbitMQ.Tests
{
    public class BrokerConfigurationTests
    {
        [Fact]
        public void SetsUsingRabbitMq()
        {
            var mockRepository = new MockRepository();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();

            using (mockRepository.Record())
            {
                Expect.Call(config.Port = AmqpTcpEndpoint.UseDefaultPort);
                Expect.Call(config.VirtualHost = "/");
                Expect.Call(config.ConnectionFactory = null)
                    .IgnoreArguments()
                    .WhenCalled(mi => Assert.IsType(typeof(BrokerConnectionFactory), mi.Arguments[0]));
                Expect.Call(config.Serializer = null)
                    .IgnoreArguments()
                    .WhenCalled(mi => Assert.IsType(typeof(JsonSerializer), mi.Arguments[0]));
            }

            using (mockRepository.Playback())
            {
                config.UseRabbitMq();   
            }
        }

        [Fact]
        public void SetsVirtualHost()
        {
            var mockRepository = new MockRepository();
            var config = mockRepository.DynamicMock<IBrokerConfiguration>();

            using (mockRepository.Record())
            {
                Expect.Call(config.VirtualHost = "/abc");
            }

            using (mockRepository.Playback())
            {
                config.VirtualHost("/abc");
            }
        }
    }
}
