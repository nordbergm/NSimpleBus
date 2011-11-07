namespace NSimpleBus
{
    public class Subscribes<T>
    {
        public interface All : ISubscriber
        {
            void Consume(T message);
        }

        public interface Some : ISubscriber
        {
            Acceptance Accept(T message);
            void Consume(T message);
        }
    }
}
