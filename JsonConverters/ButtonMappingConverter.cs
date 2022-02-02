using EverLoader.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EverLoader.JsonConverters
{
    public class ButtonMappingConverter : JsonConverter
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
                return (string.IsNullOrWhiteSpace(strValue) || strValue == "NULL") ? String.Empty : strValue.Replace("\n", " | ");
            }
            return String.Empty;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var strValue = value as string;
            writer.WriteValue(string.IsNullOrWhiteSpace(strValue) ? "NULL" : strValue.Replace(" | ","\n"));
        }
    }
}
