# SQS Lambda LocalStack Setup

This project provides a Docker Compose setup for running AWS SQS and SNS services locally using LocalStack, along with a .NET Console Application that publishes person messages to the SNS topic.

## Project Structure

```
SQSLambda/
├── docker-compose.yml          # Main orchestration file
├── setup-scripts/              # AWS resource setup scripts
│   ├── setup-aws-resources.sh  # Creates SNS topic and SQS queue
│   └── test-integration.sh     # Tests SNS→SQS message flow
├── provider/                   # .NET Console Application
│   └── PersonMessageProvider/  # Person message publisher
│       ├── Models/             # Person model
│       ├── Program.cs          # Main application logic
│       ├── run.bat/.sh         # Launch scripts
│       └── README.md           # Application documentation
└── consumer/                   # Future consumer applications
```

## Services

### LocalStack
- **Container**: `localstack-main`
- **Port**: 4566 (LocalStack Gateway)
- **Services**: SQS, SNS, Lambda, S3, CloudFormation, IAM, Logs
- **Features**: 
  - Persistence disabled for fresh start
  - Debug mode enabled
  - Docker Lambda executor

### AWS Setup
- **Container**: `aws-setup`
- **Purpose**: Automatically creates SNS topic and SQS queue with subscription
- **Resources Created**:
  - SNS Topic: `notification-topic`
  - SQS Queue: `notification-queue`
  - Subscription: SNS topic → SQS queue

### Person Message Provider (.NET Console App)
- **Location**: `provider/PersonMessageProvider/`
- **Purpose**: Publishes person messages to SNS topic
- **Features**:
  - Interactive or command-line input
  - JSON message format with timestamps
  - Message attributes for filtering
  - LocalStack integration

## Quick Start

1. **Start the services**:
   ```bash
   docker-compose up -d
   ```

2. **Check the logs** to verify setup:
   ```bash
   docker-compose logs aws-setup
   ```

3. **Test the integration**:
   ```bash
   # Run the test script inside the aws-setup container
   docker-compose exec aws-setup /setup-scripts/test-integration.sh
   ```

4. **Send person messages**:
   ```bash
   # Navigate to the provider application
   cd provider/PersonMessageProvider
   
   # Send a message with command line arguments
   ./run.bat "John" "Doe"
   
   # Or run interactively (will prompt for names)
   ./run.bat
   ```

## Person Message Format

The .NET application sends messages in the following JSON format:
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "timestamp": "2025-07-16T14:35:42.806382Z",
  "messageId": "12345678-1234-1234-1234-123456789012"
}
```

With SNS message attributes:
- `MessageType`: "PersonMessage"
- `FirstName`: Person's first name
- `LastName`: Person's last name

## Environment Variables

Key environment variables are defined in `.env`:
- `AWS_ACCESS_KEY_ID=test`
- `AWS_SECRET_ACCESS_KEY=test`
- `AWS_DEFAULT_REGION=us-east-1`
- `AWS_ENDPOINT_URL=http://localhost:4566`

## Manual Testing

### Using AWS CLI (from host machine):
```bash
# Configure AWS CLI to use LocalStack
export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test
export AWS_DEFAULT_REGION=us-east-1
export AWS_ENDPOINT_URL=http://localhost:4566

# Send message to SNS topic
aws sns publish \
    --topic-arn arn:aws:sns:us-east-1:000000000000:notification-topic \
    --message "Hello from SNS" \
    --endpoint-url http://localhost:4566

# Receive message from SQS queue
aws sqs receive-message \
    --queue-url http://localhost:4566/000000000000/notification-queue \
    --endpoint-url http://localhost:4566
```

### Using curl:
```bash
# Send message to SNS topic
curl -X POST http://localhost:4566/ \
    -H "Content-Type: application/x-amz-json-1.0" \
    -H "X-Amz-Target: AmazonSNS.Publish" \
    -d '{"TopicArn":"arn:aws:sns:us-east-1:000000000000:notification-topic","Message":"Hello from curl"}'

# Receive message from SQS queue
curl -X POST http://localhost:4566/ \
    -H "Content-Type: application/x-amz-json-1.0" \
    -H "X-Amz-Target: AmazonSQS.ReceiveMessage" \
    -d '{"QueueUrl":"http://localhost:4566/000000000000/notification-queue"}'
```

## Troubleshooting

1. **Check LocalStack health**:
   ```bash
   curl http://localhost:4566/health
   ```

2. **View container logs**:
   ```bash
   docker-compose logs localstack
   docker-compose logs aws-setup
   ```

3. **List created resources**:
   ```bash
   docker-compose exec aws-setup aws sqs list-queues --endpoint-url http://localstack:4566
   docker-compose exec aws-setup aws sns list-topics --endpoint-url http://localstack:4566
   ```

4. **Restart services**:
   ```bash
   docker-compose down
   docker-compose up -d
   ```

## Cleanup

To stop and remove all containers:
```bash
docker-compose down -v
```

To also remove the LocalStack data:
```bash
docker-compose down -v
rm -rf localstack-data
```
