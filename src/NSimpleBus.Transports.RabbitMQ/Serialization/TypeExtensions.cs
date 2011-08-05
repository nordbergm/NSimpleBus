using System;

namespace NSimpleBus.Transports.RabbitMQ.Serialization
{
    public static class TypeExtensions
    {
        public static string ToRoutingKey(this Type type)
        {
            return type.FullName;
        }
    }
}
