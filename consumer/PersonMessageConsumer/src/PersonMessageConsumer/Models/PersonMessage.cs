using System.Text.Json.Serialization;

namespace PersonMessageConsumer.Models
{
    public class PersonMessage
    {
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("lastName")]
        public string LastName { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = string.Empty;

        [JsonPropertyName("messageId")]
        public string MessageId { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{FirstName} {LastName}";
        }
    }

    public class SnsNotification
    {
        [JsonPropertyName("Type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("MessageId")]
        public string MessageId { get; set; } = string.Empty;

        [JsonPropertyName("TopicArn")]
        public string TopicArn { get; set; } = string.Empty;

        [JsonPropertyName("Subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("Message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("Timestamp")]
        public string Timestamp { get; set; } = string.Empty;

        [JsonPropertyName("MessageAttributes")]
        public Dictionary<string, SnsMessageAttribute> MessageAttributes { get; set; } = new();
    }

    public class SnsMessageAttribute
    {
        [JsonPropertyName("Type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("Value")]
        public string Value { get; set; } = string.Empty;
    }
}
