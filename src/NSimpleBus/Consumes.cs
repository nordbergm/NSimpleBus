namespace NSimpleBus
{
    public class Consumes<T> where T : class
    {
        public interface All : IConsumer
        {
            void Consume(T message);
        }
    }
}
