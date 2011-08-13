namespace NSimpleBus
{
    public class Subscribes<T>
    {
        public interface All : ISubscriber
        {
            void Consume(T message);
        }
    }
}
