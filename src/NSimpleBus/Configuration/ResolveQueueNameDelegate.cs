using System;

namespace NSimpleBus.Configuration
{
    public delegate string ResolveQueueNameDelegate(Type messageType, Type consumerType);
}