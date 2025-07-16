using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Newtonsoft.Json;
using PersonMessageProvider.Models;
using System;
using System.Threading.Tasks;

namespace PersonMessageProvider
{
    class Program
    {
        private static readonly string AWS_ENDPOINT = Environment.GetEnvironmentVariable("AWS_ENDPOINT_URL") ?? "http://localhost:4566";
        private static readonly string AWS_REGION = Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION") ?? "us-east-1";
        private static readonly string TOPIC_ARN = Environment.GetEnvironmentVariable("TOPIC_ARN") ?? "arn:aws:sns:us-east-1:000000000000:notification-topic";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Person Message Provider - Starting...");
            
            // Get person details from user input or use defaults
            var person = GetPersonFromInput(args);
            
            try
            {
                // Create SNS client configured for LocalStack
                var snsClient = CreateSnsClient();
                
                // Publish person message to SNS topic
                await PublishPersonMessage(snsClient, person);
                
                Console.WriteLine($"Successfully published message for: {person}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error publishing message: {ex.Message}");
                Environment.Exit(1);
            }
        }

        private static Person GetPersonFromInput(string[] args)
        {
            var person = new Person();

            if (args.Length >= 2)
            {
                person.FirstName = args[0];
                person.LastName = args[1];
            }
            else
            {
                // Interactive input
                Console.Write("Enter first name: ");
                person.FirstName = Console.ReadLine() ?? "John";
                
                Console.Write("Enter last name: ");
                person.LastName = Console.ReadLine() ?? "Doe";
            }

            return person;
        }

        private static AmazonSimpleNotificationServiceClient CreateSnsClient()
        {
            var config = new AmazonSimpleNotificationServiceConfig()
            {
                ServiceURL = AWS_ENDPOINT,
                UseHttp = true,
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(AWS_REGION)
            };

            return new AmazonSimpleNotificationServiceClient("test", "test", config);
        }

        private static async Task PublishPersonMessage(AmazonSimpleNotificationServiceClient snsClient, Person person)
        {
            var messageJson = JsonConvert.SerializeObject(person, Formatting.Indented);
            
            var publishRequest = new PublishRequest
            {
                TopicArn = TOPIC_ARN,
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

            Console.WriteLine($"Publishing message to topic: {TOPIC_ARN}");
            Console.WriteLine($"Message: {messageJson}");

            var response = await snsClient.PublishAsync(publishRequest);
            
            Console.WriteLine($"Message published successfully! MessageId: {response.MessageId}");
        }
    }
}
