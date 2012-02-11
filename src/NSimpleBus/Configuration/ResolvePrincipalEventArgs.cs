using System.Security.Principal;
using NSimpleBus.Transports;

namespace NSimpleBus.Configuration
{
    public class ResolvePrincipalEventArgs : PipelineEventArgs
    {
        public ResolvePrincipalEventArgs(IMessageEnvelope<object> envelope) : base(envelope)
        {
        }

        public IPrincipal Principal { get; set; }
    }
}