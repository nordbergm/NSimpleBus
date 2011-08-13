namespace NSimpleBus.Tests
{
    public class TestConsumer : Consumes<TestMessage>.All
    {
        public void Consume(TestMessage message)
        {
        }
    }
}
