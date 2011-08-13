namespace NSimpleBus.Tests
{
    public class TestSubscriber : Subscribes<TestMessage>.All
    {
        public void Consume(TestMessage message)
        {
        }
    }
}
