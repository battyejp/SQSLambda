# Person Message Consumer Lambda Function

This is a .NET 8.0 AWS Lambda function that processes SQS messages containing person data sent from SNS topics.

## Features

- **SQS Event Processing**: Subscribes to SQS queue and processes messages
- **SNS Message Parsing**: Extracts person data from SNS notification messages
- **Comprehensive Logging**: Detailed logging of SQS events and message contents
- **Error Handling**: Graceful error handling with success/failure tracking
- **Type Safety**: Uses strongly typed models for person data and SNS notifications

## Function Details

- **Handler**: `PersonMessageConsumer::PersonMessageConsumer.Function::FunctionHandler`
- **Runtime**: .NET 8.0
- **Event Source**: SQS
- **Return Type**: String (success message)

## Message Processing

The function processes messages in the following format:

### Input SQS Event
```json
{
  "Records": [
    {
      "messageId": "message-id",
      "body": "SNS notification JSON",
      "eventSource": "aws:sqs",
      "eventSourceArn": "arn:aws:sqs:us-east-1:000000000000:notification-queue"
    }
  ]
}
```

### SNS Notification Structure
```json
{
  "Type": "Notification",
  "MessageId": "sns-message-id",
  "TopicArn": "arn:aws:sns:us-east-1:000000000000:notification-topic",
  "Subject": "Person Message: John Doe",
  "Message": "{\"firstName\":\"John\",\"lastName\":\"Doe\",\"timestamp\":\"2025-07-16T14:35:42.806382Z\",\"messageId\":\"person-message-id\"}",
  "MessageAttributes": {
    "MessageType": {"Type": "String", "Value": "PersonMessage"},
    "FirstName": {"Type": "String", "Value": "John"},
    "LastName": {"Type": "String", "Value": "Doe"}
  }
}
```

### Person Message Data
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "timestamp": "2025-07-16T14:35:42.806382Z",
  "messageId": "person-message-id"
}
```

## Logging Output

The function provides comprehensive logging including:
- Complete SQS event structure
- Individual message details (ID, receipt handle, body, etc.)
- SNS notification parsing
- Person data extraction
- Message attributes
- Processing statistics

Example log output:
```
=== SQS Event Processing Started ===
Received 1 message(s)
--- Processing Individual Message ---
Message ID: test-message-id
📧 Person Message Received: John Doe
📅 Person Message Timestamp: 2025-07-16T14:35:42.806382Z
✅ Confirmed: This is a PersonMessage
=== SQS Event Processing Completed: Successfully processed 1 message(s). Failed: 0 ===
```

## Building and Testing

### Build
```bash
dotnet build
```

### Test
```bash
dotnet test
```

### Package for Deployment
```bash
dotnet lambda package
```

## LocalStack Deployment

To deploy to LocalStack:

1. Ensure LocalStack is running
2. Create the Lambda function
3. Configure SQS trigger
4. Test with person messages

## Dependencies

- `Amazon.Lambda.Core` - Core Lambda functionality
- `Amazon.Lambda.SQSEvents` - SQS event types
- `Amazon.Lambda.Serialization.SystemTextJson` - JSON serialization

## Error Handling

The function includes comprehensive error handling:
- JSON parsing errors are caught and logged
- Processing failures are tracked per message
- Returns success/failure statistics
- Continues processing remaining messages on individual failures
