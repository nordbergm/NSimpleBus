using System;
using NSimpleBus.Transports.RabbitMQ;
using RabbitMQ.Client;

namespace NSimpleBus.Configuration
{
    public static class BrokerConfigurationExtensions
    {
        public static void UseRabbitMq(this IBrokerConfiguration config)
        {
            if (config.Port <= 0)
            {
                config.Port = AmqpTcpEndpoint.UseDefaultPort;
            }

            if (string.IsNullOrEmpty(config.VirtualHost))
            {
                config.VirtualHost = "/";
            }

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
