using System;
using System.IO;
using Newtonsoft.Json;

namespace NSimpleBus.Serialization
{
    public class JsonSerializer : ISerializer
    {
        public Stream Serialize(object o)
        {
            var ms = new MemoryStream();
            var writer = new JsonTextWriter(new StreamWriter(ms));
            var serializer = Newtonsoft.Json.JsonSerializer.Create(new JsonSerializerSettings());

            serializer.Serialize(writer, o);
            writer.Flush();

            ms.Seek(0, SeekOrigin.Begin);

            return ms;
        }

        public object Deserialize(Stream s, Type asType)
        {
            var reader = new JsonTextReader(new StreamReader(s));
            var serializer = Newtonsoft.Json.JsonSerializer.Create(new JsonSerializerSettings());
            
            return serializer.Deserialize(reader, asType);
        }
    }
}
