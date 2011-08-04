using NSimpleBus.Configuration;
using RabbitMQ.Client;

namespace NSimpleBus.Transports.RabbitMQ
{
    public class BrokerConnectionFactory : IBrokerConnectionFactory
    {
        private readonly IBrokerConfiguration configuration;
        private ConnectionFactory factory;

        public BrokerConnectionFactory(IBrokerConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public IBrokerConnection CreateConnection()
        {
            if (factory == null)
            {
                this.factory = new ConnectionFactory
                {
                    UserName = configuration.UserName,
                    Password = configuration.Password,
                    VirtualHost = configuration.VirtualHost,
                    Protocol = Protocols.FromEnvironment(),
                    HostName = configuration.HostName,
                    Port = AmqpTcpEndpoint.UseDefaultPort
                };
            }

            return new BrokerConnection(factory.CreateConnection(), configuration);
        }
    }
}
