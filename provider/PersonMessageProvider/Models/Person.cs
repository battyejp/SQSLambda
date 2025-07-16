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
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonProperty("messageId")]
        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        public override string ToString()
        {
            return $"{FirstName} {LastName}";
        }
    }
}
