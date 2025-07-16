using Xunit;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.SQSEvents;
using System.Text.Json;

namespace PersonMessageConsumer.Tests;

public class FunctionTest
{
    [Fact]
    public async Task TestSQSEventLambdaFunction()
    {
        // Create a sample SQS event that mimics what would come from SNS
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    MessageId = "test-message-id",
                    Body = JsonSerializer.Serialize(new
                    {
                        Type = "Notification",
                        MessageId = "sns-message-id",
                        TopicArn = "arn:aws:sns:us-east-1:000000000000:notification-topic",
                        Subject = "Person Message: John Doe",
                        Message = JsonSerializer.Serialize(new
                        {
                            firstName = "John",
                            lastName = "Doe",
                            timestamp = "2025-07-16T14:35:42.806382Z",
                            messageId = "person-message-id"
                        }),
                        Timestamp = "2025-07-16T14:35:43.444Z",
                        MessageAttributes = new Dictionary<string, object>
                        {
                            ["MessageType"] = new { Type = "String", Value = "PersonMessage" },
                            ["FirstName"] = new { Type = "String", Value = "John" },
                            ["LastName"] = new { Type = "String", Value = "Doe" }
                        }
                    }),
                    EventSource = "aws:sqs",
                    EventSourceArn = "arn:aws:sqs:us-east-1:000000000000:notification-queue",
                    AwsRegion = "us-east-1"
                }
            }
        };

        var logger = new TestLambdaLogger();
        var context = new TestLambdaContext
        {
            Logger = logger
        };

        var function = new Function();
        var result = await function.FunctionHandler(sqsEvent, context);

        Assert.Equal("Successfully processed 1 message(s). Failed: 0", result);
        Assert.Contains("Person Message Received: John Doe", logger.Buffer.ToString());
    }
}