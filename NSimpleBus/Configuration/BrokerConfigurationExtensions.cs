using NSimpleBus.Serialization;

namespace NSimpleBus.Configuration
{
    public static class BrokerConfigurationExtensions
    {
        public static void Credentials(this IBrokerConfiguration config, string userName, string password)
        {
            config.UserName = userName;
            config.Password = password;
        }

        public static void JsonSerializer(this IBrokerConfiguration config)
        {
            config.Serializer = new JsonSerializer();
        }

        public static void Broker(this IBrokerConfiguration config, string hostname, int? port = null)
        {
            config.HostName = hostname;

            if (port.HasValue)
            {
                config.Port = port.Value;
            }
        }

        public static void Exchange(this IBrokerConfiguration config, string exhange)
        {
            config.Exchange = exhange;
        }

        public static void CompetingConsumer(this IBrokerConfiguration config)
        {
            config.AutoConfigure = AutoConfigureMode.CompetingConsumer;
        }

        public static void PublishSubscribe(this IBrokerConfiguration config)
        {
            config.AutoConfigure = AutoConfigureMode.PublishSubscribe;
        }
    }
}
