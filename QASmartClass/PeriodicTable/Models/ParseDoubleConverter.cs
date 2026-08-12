using System;
using System.Globalization;
using Newtonsoft.Json;

namespace QASmartTouch.PeriodicTable.Models
{
    /// <summary>
    /// JsonConverter that parses numbers that may be represented as strings into doubles (or nullable doubles).
    /// This makes deserialization tolerant of JSON where numeric fields are accidentally quoted.
    /// </summary>
    public class ParseDoubleConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(double) || objectType == typeof(double?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            bool isNullable = (objectType == typeof(double?));

            if (reader.TokenType == JsonToken.Null)
            {
                if (isNullable) return null;
                return 0.0;
            }

            if (reader.TokenType == JsonToken.Float || reader.TokenType == JsonToken.Integer)
            {
                return Convert.ToDouble(reader.Value);
            }

            if (reader.TokenType == JsonToken.String)
            {
                var s = (string)reader.Value;
                if (string.IsNullOrWhiteSpace(s))
                {
                    return isNullable ? (double?)null : 0.0;
                }

                // Try parse using invariant culture
                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                {
                    return d;
                }

                // Try parse with current culture as a fallback
                if (double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out d))
                {
                    return d;
                }

                // If value cannot be parsed (e.g. contains text), return null for nullable or 0 for non-nullable
                return isNullable ? (double?)null : 0.0;
            }

            // Other token types: attempt to convert
            try
            {
                return Convert.ToDouble(reader.Value);
            }
            catch
            {
                return isNullable ? (double?)null : 0.0;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            if (value is double d)
            {
                writer.WriteValue(d);
                return;
            }

            // Fallback
            writer.WriteValue(value.ToString());
        }
    }
}

