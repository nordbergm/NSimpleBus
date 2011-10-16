namespace NSimpleBus.Tests.Namespace3
{
    public class MultipleConsumer : Consumes<TestMessage>.All, Consumes<TestMessage2>.All
    {
        #region All Members

        public void Consume(TestMessage message)
        {
        }

        #endregion

        #region All Members

        public void Consume(TestMessage2 message)
        {
        }

        #endregion
    }
}