namespace NSimpleBus
{
    public class Consumes<T> where T : class
    {
        public interface All : IConsumer
        {
            void Consume(T message);
        }

        public interface Some : IConsumer
        {
            Acceptance Accept(T message);
            void Consume(T message);
        }
    }
}
