using Newtonsoft.Json;
using System;

namespace Splawft.YAMLSON
{
    internal class UnityObjectTracker : JsonConverter
    {
        private UnityDumper dumper;
        public override bool CanWrite => true;
        public override bool CanRead => false;

        public UnityObjectTracker(UnityDumper dumper)
        {
            this.dumper = dumper;
        }

        public override bool CanConvert(Type objectType) => typeof(UnityEngine.Object).IsAssignableFrom(objectType);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var obj = (UnityEngine.Object)value;
            writer.WriteStartObject();
            writer.WritePropertyName("fileID");
            if (obj == null || !dumper.TryAddComponent(obj))
                writer.WriteValue(0);
            else
                writer.WriteValue(obj.GetInstanceID());

            writer.WriteEndObject();

        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}
