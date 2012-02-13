using System.Collections.Generic;
using System.Threading;

namespace NSimpleBus.Transports
{
    public class MessageEnvelope<T> : IMessageEnvelope<T> where T : class
    {
        public MessageEnvelope(T message)
        {
            this.Message = message;
            this.Headers = new Dictionary<string, string>();

            if (Thread.CurrentPrincipal != null && Thread.CurrentPrincipal.Identity.IsAuthenticated)
            {
                this.UserName = Thread.CurrentPrincipal.Identity.Name;
            }
        }

        #region IMessageEnvelope<T> Members

        public string UserName { get; set; }
        public T Message { get; set; }
        public IDictionary<string, string> Headers { get; set; }

        #endregion
    }
}