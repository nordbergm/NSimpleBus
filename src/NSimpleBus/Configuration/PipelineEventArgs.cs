using System;
using NSimpleBus.Transports;

namespace NSimpleBus.Configuration
{
    public class PipelineEventArgs : EventArgs
    {
        public PipelineEventArgs(IMessageEnvelope<object> envelope)
        {
            MessageEnvelope = envelope;
        }

        public IMessageEnvelope<object> MessageEnvelope { get; set; }
    }
}