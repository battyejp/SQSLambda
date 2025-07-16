# SQS Lambda LocalStack Setup

This project provides a Docker Compose setup for running AWS SQS and SNS services locally using LocalStack.

## Services

### LocalStack
- **Container**: `localstack-main`
- **Port**: 4566 (LocalStack Gateway)
- **Services**: SQS, SNS, Lambda, S3, CloudFormation, IAM, Logs
- **Features**: 
  - Persistence enabled
  - Debug mode enabled
  - Docker Lambda executor

### AWS Setup
- **Container**: `aws-setup`
- **Purpose**: Automatically creates SNS topic and SQS queue with subscription
- **Resources Created**:
  - SNS Topic: `notification-topic`
  - SQS Queue: `notification-queue`
  - Subscription: SNS topic → SQS queue

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

## Project Structure

```
.
├── docker-compose.yml          # Main Docker Compose configuration
├── .env                       # Environment variables
├── setup-scripts/
│   └── test-integration.sh    # Test script for SNS/SQS integration
├── init-scripts/             # LocalStack initialization scripts
│   └── setup-aws-resources.sh
├── aws-scripts/              # Additional AWS testing scripts
│   ├── test-sqs-sns.sh
│   └── list-resources.sh
├── localstack-data/          # LocalStack data persistence
├── consumer/                 # Consumer application directory
└── provider/                 # Provider application directory
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
