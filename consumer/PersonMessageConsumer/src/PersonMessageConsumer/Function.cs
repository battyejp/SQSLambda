using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using PersonMessageConsumer.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PersonMessageConsumer;

public class Function
{
    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {

    }


    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
    /// to respond to SQS messages.
    /// </summary>
    /// <param name="evnt">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task<string> FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        context.Logger.LogInformation("=== SQS Event Processing Started ===");
        context.Logger.LogInformation($"Received {evnt.Records.Count} message(s)");

        // Log the complete SQS event structure
        try
        {
            var eventJson = JsonSerializer.Serialize(evnt, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            context.Logger.LogInformation($"Complete SQS Event: {eventJson}");
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error serializing SQS event: {ex.Message}");
        }

        int processedCount = 0;
        int failedCount = 0;

        foreach(var message in evnt.Records)
        {
            try
            {
                await ProcessMessageAsync(message, context);
                processedCount++;
            }
            catch (Exception ex)
            {
                failedCount++;
                context.Logger.LogError($"Error processing message {message.MessageId}: {ex.Message}");
            }
        }

        var result = $"Successfully processed {processedCount} message(s). Failed: {failedCount}";
        context.Logger.LogInformation($"=== SQS Event Processing Completed: {result} ===");
        
        return result;
    }

    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        context.Logger.LogInformation("--- Processing Individual Message ---");
        context.Logger.LogInformation($"Message ID: {message.MessageId}");
        context.Logger.LogInformation($"Receipt Handle: {message.ReceiptHandle}");
        context.Logger.LogInformation($"Message Body: {message.Body}");
        context.Logger.LogInformation($"MD5 of Body: {message.Md5OfBody}");
        context.Logger.LogInformation($"Event Source: {message.EventSource}");
        context.Logger.LogInformation($"Event Source ARN: {message.EventSourceArn}");
        context.Logger.LogInformation($"AWS Region: {message.AwsRegion}");

        // Log message attributes if they exist
        if (message.MessageAttributes != null && message.MessageAttributes.Count > 0)
        {
            context.Logger.LogInformation("Message Attributes:");
            foreach (var attr in message.MessageAttributes)
            {
                context.Logger.LogInformation($"  {attr.Key}: {attr.Value.StringValue} (Type: {attr.Value.DataType})");
            }
        }

        // Try to parse the message body as JSON to extract person information
        try
        {
            // The message body from SNS is wrapped in a notification structure
            var snsNotification = JsonSerializer.Deserialize<SnsNotification>(message.Body);
            
            if (snsNotification != null && !string.IsNullOrEmpty(snsNotification.Message))
            {
                context.Logger.LogInformation($"SNS Message Content: {snsNotification.Message}");
                context.Logger.LogInformation($"📬 SNS Subject: {snsNotification.Subject}");
                context.Logger.LogInformation($"📍 SNS Topic ARN: {snsNotification.TopicArn}");
                context.Logger.LogInformation($"📅 SNS Timestamp: {snsNotification.Timestamp}");
                
                // Log SNS Message Attributes
                if (snsNotification.MessageAttributes.Count > 0)
                {
                    context.Logger.LogInformation("SNS Message Attributes:");
                    foreach (var attr in snsNotification.MessageAttributes)
                    {
                        context.Logger.LogInformation($"  {attr.Key}: {attr.Value.Value} (Type: {attr.Value.Type})");
                    }
                }
                
                // Try to parse the person data
                var personMessage = JsonSerializer.Deserialize<PersonMessage>(snsNotification.Message);
                if (personMessage != null)
                {
                    // Validate that both first name and last name are present
                    if (string.IsNullOrWhiteSpace(personMessage.FirstName) || string.IsNullOrWhiteSpace(personMessage.LastName))
                    {
                        var errorMessage = $"❌ Invalid PersonMessage: Missing required fields. " +
                                         $"FirstName: '{personMessage.FirstName}', LastName: '{personMessage.LastName}'";
                        context.Logger.LogError(errorMessage);
                        throw new ArgumentException(errorMessage);
                    }

                    // Validate messageId format (must be a valid UUID)
                    if (string.IsNullOrWhiteSpace(personMessage.MessageId))
                    {
                        var errorMessage = "❌ Invalid PersonMessage: MessageId is required";
                        context.Logger.LogError(errorMessage);
                        throw new ArgumentException(errorMessage);
                    }

                    var uuidPattern = @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$";
                    if (!Regex.IsMatch(personMessage.MessageId, uuidPattern, RegexOptions.IgnoreCase))
                    {
                        var errorMessage = $"❌ Invalid PersonMessage: MessageId '{personMessage.MessageId}' is not a valid UUID format";
                        context.Logger.LogError(errorMessage);
                        throw new ArgumentException(errorMessage);
                    }

                    context.Logger.LogInformation($"📧 Person Message Received: {personMessage}");
                    context.Logger.LogInformation($"📅 Person Message Timestamp: {personMessage.Timestamp}");
                    context.Logger.LogInformation($"🆔 Person Message ID: {personMessage.MessageId}");
                    
                    // Check for PersonMessage type using SNS attributes
                    if (snsNotification.MessageAttributes.TryGetValue("MessageType", out var messageType))
                    {
                        if (messageType.Value == "PersonMessage")
                        {
                            context.Logger.LogInformation("✅ Confirmed: This is a PersonMessage");
                            
                            // Extract additional attributes
                            if (snsNotification.MessageAttributes.TryGetValue("FirstName", out var firstName))
                            {
                                context.Logger.LogInformation($"👤 First Name (from attributes): {firstName.Value}");
                            }
                            if (snsNotification.MessageAttributes.TryGetValue("LastName", out var lastName))
                            {
                                context.Logger.LogInformation($"👤 Last Name (from attributes): {lastName.Value}");
                            }
                        }
                    }
                }
                else
                {
                    var errorMessage = "❌ Failed to deserialize PersonMessage: Message content is null or invalid";
                    context.Logger.LogError(errorMessage);
                    throw new ArgumentException(errorMessage);
                }
            }
            else
            {
                var errorMessage = "❌ SNS notification message is empty or invalid";
                context.Logger.LogError(errorMessage);
                throw new ArgumentException(errorMessage);
            }
        }
        catch (ArgumentException ex)
        {
            context.Logger.LogError($"Validation error: {ex.Message}");
            throw; // Re-throw validation exceptions to fail the Lambda function
        }
        catch (JsonException ex)
        {
            context.Logger.LogError($"Error parsing JSON message: {ex.Message}");
            throw; // Re-throw JSON parsing exceptions to fail the Lambda function
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error processing message content: {ex.Message}");
            throw; // Re-throw all other exceptions to fail the Lambda function
        }

        context.Logger.LogInformation("--- Message Processing Complete ---");
        
        await Task.CompletedTask;
    }
}