using System.Threading;

namespace NSimpleBus.Transports
{
    public class MessageEnvelope<T> : IMessageEnvelope<T> where T : class
    {
        public MessageEnvelope(T message)
        {
            this.Message = message;
            this.MessageType = message.GetType().AssemblyQualifiedName;

            if (Thread.CurrentPrincipal != null && Thread.CurrentPrincipal.Identity.IsAuthenticated)
            {
                this.UserName = Thread.CurrentPrincipal.Identity.Name;
            }
        }

        #region IMessageEnvelope<T> Members

        public string UserName { get; private set; }
        public T Message { get; private set; }
        public string MessageType { get; private set; }

        #endregion
    }
}