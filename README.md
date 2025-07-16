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
    └── PersonMessageConsumer/  # Person message processor
        ├── Models/             # Person model
        ├── Function.cs         # Lambda function logic
        ├── deploy-lambda.bat/.sh # Deployment scripts
        └── README.md           # Application documentation
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

### Person Message Consumer (.NET Lambda Function)
- **Location**: `consumer/PersonMessageConsumer/`
- **Purpose**: Processes person messages from SQS queue
- **Features**:
  - Comprehensive SQS event logging
  - SNS notification parsing
  - Person message extraction
  - Error handling and statistics
  - Automatic deployment to LocalStack

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

5. **Deploy the consumer Lambda function**:
   ```bash
   # Navigate to the setup scripts
   cd setup-scripts
   
   # Deploy the Lambda function
   ./deploy-lambda.bat
   ```

6. **Send a test message** (if not done already):
   ```bash
   # Navigate to the provider application
   cd ../provider/PersonMessageProvider
   
   # Send a test message
   ./run.bat "Jane" "Doe"
   ```

7. **Check Lambda logs**:
   ```bash
   # View the last 10 logs for the PersonMessage
   docker logs localstack-main | Select-String "PersonMessage" | Select-Object -Last 10
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

## Complete Message Flow

1. **Provider** → Sends person message to SNS topic
2. **SNS Topic** → Forwards message to SQS queue  
3. **SQS Queue** → Triggers Lambda function
4. **Lambda Consumer** → Processes message and logs details

## Quick Demo

```bash
# Start the entire system
docker-compose up -d

# Deploy the Lambda function
cd setup-scripts
./deploy-lambda.bat

# Send a person message
cd ../provider/PersonMessageProvider
./run.bat "John" "Doe"

# Check Lambda logs
docker logs localstack-main | Select-String "PersonMessage" | Select-Object -Last 10
```

## Lambda Deployment

The Lambda function can be deployed using:
```bash
cd setup-scripts
./deploy-lambda.bat  # Windows
./deploy-lambda.sh   # Linux/Mac
```

This will:
- Build and package the Lambda function
- Deploy to LocalStack
- Create SQS event source mapping
- Test the function
