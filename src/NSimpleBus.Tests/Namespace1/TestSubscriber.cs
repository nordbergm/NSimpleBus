namespace NSimpleBus.Tests.Namespace1
{
    public class TestSubscriber : Subscribes<TestMessage>.All
    {
        public void Consume(TestMessage message)
        {
        }
    }
}
