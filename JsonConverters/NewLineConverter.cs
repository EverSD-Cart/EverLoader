using Newtonsoft.Json;
using System;

namespace EverLoader.JsonConverters
{
    public class NewLineFixConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                var strValue = reader.Value?.ToString();
                return strValue == null ? String.Empty : strValue.Replace("\n", Environment.NewLine);
            }
            return String.Empty;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var strValue = value as string;
            writer.WriteValue(string.IsNullOrWhiteSpace(strValue) ? String.Empty : strValue.Replace(Environment.NewLine, "\n"));
        }
    }
}
