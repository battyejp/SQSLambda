using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;
using PactNet;
using PactNet.Infrastructure.Outputters;
using PactNet.Matchers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace PersonMessageConsumer.Tests;

/// <summary>
/// Consumer-driven contract tests using messaging Pacts for Lambda functions
/// This implementation uses the proper IMessagePactBuilderV4 interface for PactNet v4
/// </summary>
public class MessagingPactTests : IDisposable
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly Function _function;
    private readonly TestLambdaContext _context;
    private readonly string _pactDir;
    private readonly IMessagePactBuilderV4 _messagePactBuilder;

    public MessagingPactTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        _function = new Function();
        _context = new TestLambdaContext();
        
        // Set pact directory to be in the test project root, not in bin
        var testProjectRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".."));
        _pactDir = Path.Combine(testProjectRoot, "pacts");
        
        // Ensure pact directory exists
        if (!Directory.Exists(_pactDir))
        {
            Directory.CreateDirectory(_pactDir);
        }

        // Create the PactNet V4 message builder
        var pactConfig = new PactConfig
        {
            PactDir = _pactDir,
            // Remove custom outputter for now - PactNet will use default console output
            DefaultJsonSettings = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }
        };

        _messagePactBuilder = Pact.V4("PersonMessageConsumerLambda", "PersonMessageProvider", pactConfig)
            .WithMessageInteractions();
    }

    [Fact]
    public async Task Lambda_Should_Successfully_Process_Valid_PersonMessage()
    {
        // Arrange - Define the expected message structure using IMessagePactBuilderV4
        var messageInteraction = _messagePactBuilder
            .ExpectsToReceive("a valid person message")
            .WithJsonContent(new
            {
                firstName = Match.Type("John"),
                lastName = Match.Type("Doe"),
                timestamp = Match.Regex("2025-07-16T16:30:00.000000Z", @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{6}Z$"),
                messageId = Match.Regex("12345678-1234-1234-1234-123456789012", @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$")
            });

        // Act & Assert - Test the Lambda function with the message
        await messageInteraction.VerifyAsync<object>(async (message) =>
        {
            var sqsEvent = CreateSQSEventWithPersonMessage(message);
            var result = await _function.FunctionHandler(sqsEvent, _context);
            
            // Assert - Verify the Lambda processed the message without errors
            Assert.NotNull(result);
            Assert.Contains("Successfully processed 1 message(s)", result);
            Assert.Contains("Failed: 0", result);
            
            _outputHelper.WriteLine($"Lambda processed valid message successfully: {result}");
        });
    }

    [Fact]
    public async Task Lambda_Should_Reject_Message_Missing_FirstName()
    {
        // Arrange - Define a message structure missing firstName using IMessagePactBuilderV4
        var messageInteraction = _messagePactBuilder
            .ExpectsToReceive("a person message missing firstName")
            .WithJsonContent(new
            {
                lastName = Match.Type("Doe"),
                timestamp = Match.Regex("2025-07-16T16:30:00.000000Z", @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{6}Z$"),
                messageId = Match.Regex("12345678-1234-1234-1234-123456789012", @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$")
            });

        // Act & Assert - Test the Lambda function with the invalid message
        await messageInteraction.VerifyAsync<object>(async (message) =>
        {
            var sqsEvent = CreateSQSEventWithPersonMessage(message);
            var result = await _function.FunctionHandler(sqsEvent, _context);
            
            // Assert - Verify the result shows the message failed validation
            Assert.NotNull(result);
            Assert.Contains("Successfully processed 0 message(s)", result);
            Assert.Contains("Failed: 1", result);
            
            _outputHelper.WriteLine($"Lambda correctly rejected invalid message: {result}");
        });
    }

    [Fact]
    public async Task Lambda_Should_Reject_Message_Missing_LastName()
    {
        // Arrange - Define a message structure missing lastName using IMessagePactBuilderV4
        var messageInteraction = _messagePactBuilder
            .ExpectsToReceive("a person message missing lastName")
            .WithJsonContent(new
            {
                firstName = Match.Type("John"),
                timestamp = Match.Regex("2025-07-16T16:30:00.000000Z", @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{6}Z$"),
                messageId = Match.Regex("12345678-1234-1234-1234-123456789012", @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$")
            });

        // Act & Assert - Test the Lambda function with the invalid message
        await messageInteraction.VerifyAsync<object>(async (message) =>
        {
            var sqsEvent = CreateSQSEventWithPersonMessage(message);
            var result = await _function.FunctionHandler(sqsEvent, _context);
            
            // Assert - Verify the result shows the message failed validation
            Assert.NotNull(result);
            Assert.Contains("Successfully processed 0 message(s)", result);
            Assert.Contains("Failed: 1", result);
            
            _outputHelper.WriteLine($"Lambda correctly rejected invalid message: {result}");
        });
    }

    [Fact]
    public async Task Lambda_Should_Handle_PersonMessage_With_All_Fields()
    {
        // Arrange - Define a message structure with all possible fields using IMessagePactBuilderV4
        var messageInteraction = _messagePactBuilder
            .ExpectsToReceive("a complete person message with all fields")
            .WithJsonContent(new
            {
                firstName = Match.Type("John"),
                lastName = Match.Type("Doe"),
                timestamp = Match.Regex("2025-07-16T16:30:00.000000Z", @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{6}Z$"),
                messageId = Match.Regex("12345678-1234-1234-1234-123456789012", @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$")
            });

        // Act & Assert - Test the Lambda function with the complete message
        await messageInteraction.VerifyAsync<object>(async (message) =>
        {
            var sqsEvent = CreateSQSEventWithPersonMessage(message);
            var result = await _function.FunctionHandler(sqsEvent, _context);
            
            // Assert - Verify the Lambda processed the message without errors
            Assert.NotNull(result);
            Assert.Contains("Successfully processed 1 message(s)", result);
            Assert.Contains("Failed: 0", result);
            
            _outputHelper.WriteLine($"Lambda processed complete message successfully: {result}");
        });
    }

    public void Dispose()
    {
        // The IMessagePactBuilderV4 will automatically generate the pact file
        // when the test class is disposed
        _outputHelper.WriteLine("IMessagePactBuilderV4 disposing - pact file will be generated automatically");
    }

    private static SQSEvent CreateSQSEventWithPersonMessage(object personMessage)
    {
        var snsNotification = new
        {
            Type = "Notification",
            MessageId = Guid.NewGuid().ToString(),
            TopicArn = "arn:aws:sns:us-east-1:123456789012:PersonMessageTopic",
            Subject = "PersonMessage",
            Message = JsonSerializer.Serialize(personMessage),
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"),
            MessageAttributes = new Dictionary<string, object>
            {
                ["MessageType"] = new { Type = "String", Value = "PersonMessage" }
            }
        };

        return new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    MessageId = Guid.NewGuid().ToString(),
                    Body = JsonSerializer.Serialize(snsNotification),
                    ReceiptHandle = "test-receipt-handle",
                    Attributes = new Dictionary<string, string>(),
                    MessageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>(),
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue",
                    EventSource = "aws:sqs",
                    AwsRegion = "us-east-1"
                }
            }
        };
    }
}
