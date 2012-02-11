using System;

namespace NSimpleBus.Configuration
{
    public class PipelineEvents : IPipelineEvents
    {
        #region IPipelineEvents Members

        public event EventHandler<PipelineEventArgs> MessageReceived;
        public event EventHandler<PipelineEventArgs> MessageConsumed;
        public event EventHandler<PipelineEventArgs> MessageSending;
        public event EventHandler<ResolvePrincipalEventArgs> ResolvePrincipal;

        public void OnMessageSending(PipelineEventArgs e)
        {
            EventHandler<PipelineEventArgs> handler = MessageSending;
            if (handler != null) handler(this, e);
        }

        public void OnResolvePrincipal(ResolvePrincipalEventArgs e)
        {
            EventHandler<ResolvePrincipalEventArgs> handler = ResolvePrincipal;
            if (handler != null) handler(this, e);
        }

        public void OnMessageReceived(PipelineEventArgs e)
        {
            EventHandler<PipelineEventArgs> handler = MessageReceived;
            if (handler != null) handler(this, e);
        }

        public void OnMessageConsumed(PipelineEventArgs e)
        {
            EventHandler<PipelineEventArgs> handler = MessageConsumed;
            if (handler != null) handler(this, e);
        }

        #endregion
    }
}