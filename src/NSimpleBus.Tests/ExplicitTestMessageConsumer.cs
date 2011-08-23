namespace NSimpleBus.Tests
{
    public class ExplicitTestMessageConsumer : Consumes<TestMessage>.All
    {
        void Consumes<TestMessage>.All.Consume(TestMessage message)
        {
        }
    }
}
