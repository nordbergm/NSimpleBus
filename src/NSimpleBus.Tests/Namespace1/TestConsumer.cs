namespace NSimpleBus.Tests.Namespace1
{
    public class TestConsumer : Consumes<TestMessage>.All
    {
        public void Consume(TestMessage message)
        {
        }
    }
}
