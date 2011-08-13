namespace NSimpleBus.Tests.Namespace2
{
    public class TestConsumer2 : Consumes<TestMessage>.All
    {
        public void Consume(TestMessage message)
        {
        }
    }
}
