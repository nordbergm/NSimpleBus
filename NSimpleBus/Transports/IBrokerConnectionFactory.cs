namespace NSimpleBus.Transports
{
    public interface IBrokerConnectionFactory
    {
        IBrokerConnection CreateConnection();
    }
}
