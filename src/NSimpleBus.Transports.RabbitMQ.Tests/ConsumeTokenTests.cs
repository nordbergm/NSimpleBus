using RabbitMQ.Client;
using Rhino.Mocks;
using Xunit;

namespace NSimpleBus.Transports.RabbitMQ.Tests
{
    public class ConsumeTokenTests
    {
        [Fact]
        public void IsNotClosedAtCreation()
        {
            var mockRepository = new MockRepository();
            var rabbitModel = mockRepository.DynamicMock<IModel>();

            var consumeToken = new ConsumeToken("tag", rabbitModel);

            Assert.False(consumeToken.IsClosed);
        }

        [Fact]
        public void IsClosedAfterClose()
        {
            var mockRepository = new MockRepository();
            var rabbitModel = mockRepository.DynamicMock<IModel>();

            var consumeToken = new ConsumeToken("tag", rabbitModel);
            consumeToken.Close();

            Assert.True(consumeToken.IsClosed);
        }

        [Fact]
        public void IsClosedAfterDispose()
        {
            var mockRepository = new MockRepository();
            var rabbitModel = mockRepository.DynamicMock<IModel>();

            var consumeToken = new ConsumeToken("tag", rabbitModel);
            consumeToken.Dispose();

            Assert.True(consumeToken.IsClosed);
        }

        [Fact]
        public void CancelsOnClose()
        {
            var mockRepository = new MockRepository();
            var rabbitModel = mockRepository.DynamicMock<IModel>();

            using (mockRepository.Record())
            {
                Expect.Call(() => rabbitModel.BasicCancel("tag"));
            }

            using (mockRepository.Playback())
            {
                var consumeToken = new ConsumeToken("tag", rabbitModel);
                consumeToken.Close();
            }
        }
    }
}
