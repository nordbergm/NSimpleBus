using System;

namespace NSimpleBus.Configuration
{
    public interface IPipelineEvents
    {
        event EventHandler<PipelineEventArgs> MessageReceived;
        event EventHandler<PipelineEventArgs> MessageConsumed;
        event EventHandler<ResolvePrincipalEventArgs> ResolvePrincipal;
        event EventHandler<PipelineEventArgs> MessageSending;
        void OnResolvePrincipal(ResolvePrincipalEventArgs e);
        void OnMessageReceived(PipelineEventArgs e);
        void OnMessageConsumed(PipelineEventArgs e);
        void OnMessageSending(PipelineEventArgs e);
    }
}