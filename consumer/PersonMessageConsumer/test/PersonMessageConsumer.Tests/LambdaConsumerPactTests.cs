using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;
using PactNet;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace PersonMessageConsumer.Tests;

/// <summary>
/// Consumer-driven contract tests where the Lambda function acts as the consumer
/// These tests verify that the Lambda can properly consume messages from the provider
/// </summary>
public class LambdaConsumerPactTests
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly Function _function;
    private readonly TestLambdaContext _context;
    private readonly string _pactDir;

    public LambdaConsumerPactTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        _function = new Function();
        _context = new TestLambdaContext();
        _pactDir = Path.Combine(Directory.GetCurrentDirectory(), "pacts");
        Directory.CreateDirectory(_pactDir);
    }

    [Fact]
    public async Task Lambda_Should_Successfully_Process_Valid_PersonMessage()
    {
        // Arrange - Valid person message
        var validPersonMessage = new
        {
            firstName = "John",
            lastName = "Doe",
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"),
            messageId = Guid.NewGuid().ToString()
        };

        // Act - Test the Lambda function directly
        var sqsEvent = CreateSQSEventWithPersonMessage(validPersonMessage);
        
        // The Lambda should process this message successfully
        var result = await _function.FunctionHandler(sqsEvent, _context);
        
        // Assert - Verify the Lambda processed the message without throwing an exception
        Assert.NotNull(result);
        Assert.Contains("Successfully processed 1 message(s)", result);
        Assert.Contains("Failed: 0", result);
        
        // Document the contract expectation
        var contractExpectation = new
        {
            consumer = "PersonMessageConsumerLambda",
            provider = "PersonMessageProvider",
            interaction = new
            {
                description = "Lambda processes valid person message",
                request = new
                {
                    method = "POST",
                    path = "/message",
                    headers = new { ContentType = "application/json" },
                    body = validPersonMessage
                },
                response = new
                {
                    status = 200,
                    body = new { status = "processed", message = "Successfully processed person message" }
                }
            }
        };

        var contractJson = JsonSerializer.Serialize(contractExpectation, new JsonSerializerOptions { WriteIndented = true });
        var contractFile = Path.Combine(_pactDir, "PersonMessageConsumerLambda-PersonMessageProvider-ValidMessage.json");
        File.WriteAllText(contractFile, contractJson);
        
        _outputHelper.WriteLine($"Contract documented: {contractFile}");
    }

    [Fact]
    public async Task Lambda_Should_Reject_Message_Missing_FirstName()
    {
        // Arrange - Message with empty firstName should cause validation error
        var invalidMessage = new
        {
            firstName = "", // Explicitly empty
            lastName = "Doe",
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"),
            messageId = Guid.NewGuid().ToString()
        };

        // Act - Test the Lambda function
        var sqsEvent = CreateSQSEventWithPersonMessage(invalidMessage);
        
        // The Lambda should process the message but it should fail validation
        var result = await _function.FunctionHandler(sqsEvent, _context);
        
        // Assert - Verify the result shows the message failed
        Assert.NotNull(result);
        Assert.Contains("Successfully processed 0 message(s)", result);
        Assert.Contains("Failed: 1", result);
        
        // Document the contract expectation
        var contractExpectation = new
        {
            consumer = "PersonMessageConsumerLambda",
            provider = "PersonMessageProvider",
            interaction = new
            {
                description = "Lambda rejects message missing firstName",
                request = new
                {
                    method = "POST",
                    path = "/message",
                    headers = new { ContentType = "application/json" },
                    body = invalidMessage
                },
                response = new
                {
                    status = 400,
                    body = new { error = "firstName is required" }
                }
            }
        };

        var contractJson = JsonSerializer.Serialize(contractExpectation, new JsonSerializerOptions { WriteIndented = true });
        var contractFile = Path.Combine(_pactDir, "PersonMessageConsumerLambda-PersonMessageProvider-MissingFirstName.json");
        File.WriteAllText(contractFile, contractJson);
        
        _outputHelper.WriteLine($"Contract documented: {contractFile}");
    }

    [Fact]
    public async Task Lambda_Should_Reject_Message_Missing_LastName()
    {
        // Arrange - Message with empty lastName should cause validation error
        var invalidMessage = new
        {
            firstName = "John",
            lastName = "", // Explicitly empty
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"),
            messageId = Guid.NewGuid().ToString()
        };

        // Act - Test the Lambda function
        var sqsEvent = CreateSQSEventWithPersonMessage(invalidMessage);
        
        // The Lambda should process the message but it should fail validation
        var result = await _function.FunctionHandler(sqsEvent, _context);
        
        // Assert - Verify the result shows the message failed
        Assert.NotNull(result);
        Assert.Contains("Successfully processed 0 message(s)", result);
        Assert.Contains("Failed: 1", result);
        
        // Document the contract expectation
        var contractExpectation = new
        {
            consumer = "PersonMessageConsumerLambda",
            provider = "PersonMessageProvider",
            interaction = new
            {
                description = "Lambda rejects message missing lastName",
                request = new
                {
                    method = "POST",
                    path = "/message",
                    headers = new { ContentType = "application/json" },
                    body = invalidMessage
                },
                response = new
                {
                    status = 400,
                    body = new { error = "lastName is required" }
                }
            }
        };

        var contractJson = JsonSerializer.Serialize(contractExpectation, new JsonSerializerOptions { WriteIndented = true });
        var contractFile = Path.Combine(_pactDir, "PersonMessageConsumerLambda-PersonMessageProvider-MissingLastName.json");
        File.WriteAllText(contractFile, contractJson);
        
        _outputHelper.WriteLine($"Contract documented: {contractFile}");
    }

    [Fact]
    public async Task Lambda_Should_Handle_Multiple_Messages_In_Batch()
    {
        // Arrange - Multiple valid messages
        var validMessage1 = new
        {
            firstName = "John",
            lastName = "Doe",
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"),
            messageId = Guid.NewGuid().ToString()
        };

        var validMessage2 = new
        {
            firstName = "Jane",
            lastName = "Smith",
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"),
            messageId = Guid.NewGuid().ToString()
        };

        // Act - Test the Lambda function
        var sqsEvent = CreateSQSEventWithPersonMessages(new[] { validMessage1, validMessage2 });
        
        // The Lambda should process all messages successfully
        var result = await _function.FunctionHandler(sqsEvent, _context);
        
        // Assert - Verify processing
        Assert.NotNull(result);
        Assert.Contains("Successfully processed 2 message(s)", result);
        Assert.Contains("Failed: 0", result);
        
        // Document the contract expectation
        var contractExpectation = new
        {
            consumer = "PersonMessageConsumerLambda",
            provider = "PersonMessageProvider",
            interaction = new
            {
                description = "Lambda processes multiple valid messages",
                request = new
                {
                    method = "POST",
                    path = "/message/batch",
                    headers = new { ContentType = "application/json" },
                    body = new[] { validMessage1, validMessage2 }
                },
                response = new
                {
                    status = 200,
                    body = new { status = "processed", count = 2 }
                }
            }
        };

        var contractJson = JsonSerializer.Serialize(contractExpectation, new JsonSerializerOptions { WriteIndented = true });
        var contractFile = Path.Combine(_pactDir, "PersonMessageConsumerLambda-PersonMessageProvider-BatchMessages.json");
        File.WriteAllText(contractFile, contractJson);
        
        _outputHelper.WriteLine($"Contract documented: {contractFile}");
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

    private static SQSEvent CreateSQSEventWithPersonMessages(object[] personMessages)
    {
        var sqsMessages = new List<SQSEvent.SQSMessage>();

        foreach (var personMessage in personMessages)
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

            sqsMessages.Add(new SQSEvent.SQSMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                Body = JsonSerializer.Serialize(snsNotification),
                ReceiptHandle = "test-receipt-handle",
                Attributes = new Dictionary<string, string>(),
                MessageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>(),
                EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue",
                EventSource = "aws:sqs",
                AwsRegion = "us-east-1"
            });
        }

        return new SQSEvent
        {
            Records = sqsMessages
        };
    }
}
