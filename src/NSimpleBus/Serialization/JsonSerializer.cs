using System;
using System.IO;

namespace NSimpleBus.Serialization
{
    public class JsonSerializer : ISerializer
    {
        public Stream Serialize(object o)
        {
            Stream s = new MemoryStream();
            ServiceStack.Text.JsonSerializer.SerializeToStream(o, s);

            s.Seek(0, SeekOrigin.Begin);

            return s;
        }

        public object Deserialize(Stream s, Type asType)
        {
            return ServiceStack.Text.JsonSerializer.DeserializeFromStream(asType, s);
        }
    }
}
