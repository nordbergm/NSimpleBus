using NSimpleBus.Transports.RabbitMQ;
using RabbitMQ.Client;

namespace NSimpleBus.Configuration
{
    public static class BrokerConfigurationExtensions
    {
        public static void UseRabbitMq(this IBrokerConfiguration config)
        {
            config.Port = AmqpTcpEndpoint.UseDefaultPort;
            config.VirtualHost = "/";
            config.ConnectionFactory = new BrokerConnectionFactory(config);
            config.JsonSerializer();
        }

        public static void VirtualHost(this IBrokerConfiguration config, string virtualHost)
        {
            config.VirtualHost = virtualHost;
        }
    }
}
