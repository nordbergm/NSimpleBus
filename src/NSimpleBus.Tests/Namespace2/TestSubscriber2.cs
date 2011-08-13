namespace NSimpleBus.Tests.Namespace2
{
    public class TestSubscriber2 : Subscribes<TestMessage>.All
    {
        public void Consume(TestMessage message)
        {
        }
    }
}
