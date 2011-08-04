using System;
using System.IO;

namespace NSimpleBus.Serialization
{
    public interface ISerializer
    {
        Stream Serialize(object o);
        object Deserialize(Stream s, Type asType);
    }
}
