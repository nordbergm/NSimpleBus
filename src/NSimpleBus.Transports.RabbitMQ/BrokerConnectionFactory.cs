using NSimpleBus.Configuration;
using NSimpleBus.Transports.RabbitMQ.Serialization;
using RabbitMQ.Client;

namespace NSimpleBus.Transports.RabbitMQ
{
    public class BrokerConnectionFactory : IBrokerConnectionFactory
    {
        private readonly IBrokerConfiguration _configuration;
        private ConnectionFactory _factory;

        public BrokerConnectionFactory(IBrokerConfiguration configuration)
        {
            this._configuration = configuration;
        }

        public IBrokerConnection CreateConnection()
        {
            if (this._factory == null)
            {
                this._factory = new ConnectionFactory
                {
                    UserName = this._configuration.UserName,
                    Password = this._configuration.Password,
                    VirtualHost = this._configuration.VirtualHost,
                    Protocol = Protocols.FromEnvironment(),
                    HostName = this._configuration.HostName,
                    Port = AmqpTcpEndpoint.UseDefaultPort,
                    RequestedHeartbeat = (ushort)this._configuration.HeartbeatInterval.TotalSeconds
                };
            }

            IConnection connection = this._factory.CreateConnection();
            IModel model = connection.CreateModel();
            IMessageSerializer serializer = new MessageSerializer(this._configuration.Serializer);

            return new BrokerConnection(
                        connection, 
                        model, 
                        this._configuration,
                        serializer, 
                        new GroupedCallbackConsumer(model, serializer, this._configuration));
        }
    }
}
