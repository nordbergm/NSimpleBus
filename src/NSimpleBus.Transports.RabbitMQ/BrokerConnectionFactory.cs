using NSimpleBus.Configuration;
using NSimpleBus.Transports.RabbitMQ.Serialization;
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

            IConnection connection = factory.CreateConnection();
            IModel model = connection.CreateModel();
            IMessageSerializer serializer = new MessageSerializer(configuration.Serializer);

            return new BrokerConnection(
                        connection, 
                        model, 
                        configuration,
                        serializer, 
                        new GroupedCallbackConsumer(model, serializer));
        }
    }
}
