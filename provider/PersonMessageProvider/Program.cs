using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Newtonsoft.Json;
using PersonMessageProvider.Models;
using PersonMessageProvider.Services;
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
                
                // Create the client and publish person message
                var client = new PersonMessageClient(snsClient, TOPIC_ARN);
                var messageId = await client.PublishPersonMessageAsync(person);
                
                Console.WriteLine($"Successfully published message for: {person}");
                Console.WriteLine($"Message ID: {messageId}");
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
    }
}
