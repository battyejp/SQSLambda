# Person Message Provider

This .NET console application publishes person messages to an SNS topic in LocalStack.

## Configuration

The application uses the following environment variables:
- `AWS_ENDPOINT_URL`: LocalStack endpoint (default: http://localhost:4566)
- `AWS_DEFAULT_REGION`: AWS region (default: us-east-1)
- `TOPIC_ARN`: SNS topic ARN (default: arn:aws:sns:us-east-1:000000000000:notification-topic)

## Running the Application

### With Docker Compose (Recommended)

1. Make sure LocalStack is running from the project root:
   ```bash
   docker-compose up -d
   ```

2. Run the application:
   ```bash
   # Using the batch script (Windows)
   .\run.bat "John" "Doe"
   
   # Using the shell script (Linux/Mac)
   ./run.sh "John" "Doe"
   
   # Or with dotnet directly
   dotnet run "John" "Doe"
   ```

### Command Line Arguments

You can pass the first name and last name as command line arguments:
```bash
.\run.bat "John" "Doe"
```

### Interactive Mode

If no arguments are provided, the application will prompt for input:
```bash
.\run.bat
Enter first name: John
Enter last name: Doe
```

## Message Format

The application sends a JSON message with the following structure:
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "timestamp": "2025-07-16T14:35:42.806382Z",
  "messageId": "12345678-1234-1234-1234-123456789012"
}
```

## Message Attributes

The SNS message includes the following attributes:
- `MessageType`: Always set to "PersonMessage"
- `FirstName`: Person's first name
- `LastName`: Person's last name

## Testing

After running the application, you can verify the messages were received by checking the SQS queue:

```bash
# From the project root directory
docker exec aws-setup /bin/bash -c "aws --endpoint-url=http://localstack:4566 sqs receive-message --queue-url http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/notification-queue --max-number-of-messages 10"
```

## Dependencies

- .NET 8.0 or later
- AWSSDK.SimpleNotificationService
- Newtonsoft.Json

## Build

```bash
dotnet build
```

## Example Usage

```bash
# Example 1: Command line arguments
.\run.bat "Alice" "Johnson"

# Example 2: Interactive mode
.\run.bat
# Enter first name: Bob
# Enter last name: Smith

# Example 3: Multiple messages
.\run.bat "Charlie" "Brown"
.\run.bat "Diana" "Prince"
```
