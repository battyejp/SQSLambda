using Newtonsoft.Json;

namespace PersonMessageProvider.Models
{
    public class Person
    {
        [JsonProperty("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonProperty("lastName")]
        public string LastName { get; set; } = string.Empty;

        [JsonProperty("timestamp")]
        [JsonConverter(typeof(UtcTimestampConverter))]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonProperty("messageId")]
        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        public override string ToString()
        {
            return $"{FirstName} {LastName}";
        }
    }

    public class UtcTimestampConverter : JsonConverter<DateTime>
    {
        public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
        {
            // Convert to UTC if not already and format as required by pact
            var utcTime = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            var formatted = utcTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ");
            writer.WriteValue(formatted);
        }

        public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var value = reader.Value?.ToString();
            if (DateTime.TryParse(value, out var result))
            {
                return result.ToUniversalTime();
            }
            return DateTime.UtcNow;
        }
    }
}
