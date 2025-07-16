using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Newtonsoft.Json;
using PersonMessageProvider.Models;

namespace PersonMessageProvider.Services
{
    public class PersonMessageClient
    {
        private readonly AmazonSimpleNotificationServiceClient? _snsClient;
        private readonly string _topicArn;

        public PersonMessageClient(AmazonSimpleNotificationServiceClient? snsClient, string topicArn)
        {
            _snsClient = snsClient; // Allow null for testing
            _topicArn = topicArn ?? throw new ArgumentNullException(nameof(topicArn));
        }

        public async Task<string> PublishPersonMessageAsync(Person person)
        {
            if (person == null)
                throw new ArgumentNullException(nameof(person));

            if (_snsClient == null)
                throw new InvalidOperationException("SNS client is required for publishing messages");

            var messageJson = JsonConvert.SerializeObject(person, Formatting.Indented);
            
            var publishRequest = new PublishRequest
            {
                TopicArn = _topicArn,
                Message = messageJson,
                Subject = $"Person Message: {person.FirstName} {person.LastName}",
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {
                        "MessageType", new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = "PersonMessage"
                        }
                    },
                    {
                        "FirstName", new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = person.FirstName
                        }
                    },
                    {
                        "LastName", new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = person.LastName
                        }
                    }
                }
            };

            var response = await _snsClient.PublishAsync(publishRequest);
            return response.MessageId;
        }

        public string GeneratePersonMessage(Person person)
        {
            if (person == null)
                throw new ArgumentNullException(nameof(person));

            return JsonConvert.SerializeObject(person, Formatting.None);
        }
    }
}
