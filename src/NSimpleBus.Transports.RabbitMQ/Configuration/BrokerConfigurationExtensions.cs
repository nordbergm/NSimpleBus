using System;
using NSimpleBus.Transports.RabbitMQ;
using NSimpleBus.Transports.RabbitMQ.Serialization;
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

        internal static string InternalExchange(this IBrokerConfiguration config, Type messageType)
        {
            if (config.AutoConfigure == AutoConfigureMode.PublishSubscribe)
            {
                return string.Format("{0}.{1}", config.Exchange, messageType.FullName);
            }

            return config.Exchange;
        }
    }
}
