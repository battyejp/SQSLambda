#!/bin/bash

# AWS Setup script for LocalStack
# Creates SNS topic and SQS queue with subscription

set -e  # Exit on any error

echo 'Waiting for LocalStack to be ready...'
until curl -s http://localstack:4566/health > /dev/null 2>&1; do
    sleep 2
done

echo 'LocalStack is ready. Setting up AWS resources...'

# Create SQS Queue
echo 'Creating SQS queue...'
aws sqs create-queue \
    --queue-name notification-queue \
    --endpoint-url http://localstack:4566

# Create SNS Topic
echo 'Creating SNS topic...'
aws sns create-topic \
    --name notification-topic \
    --endpoint-url http://localstack:4566

# Get queue ARN
QUEUE_ARN=$(aws sqs get-queue-attributes \
    --queue-url http://localstack:4566/000000000000/notification-queue \
    --attribute-names QueueArn \
    --endpoint-url http://localstack:4566 \
    --query 'Attributes.QueueArn' \
    --output text)

# Get topic ARN
TOPIC_ARN=$(aws sns list-topics \
    --endpoint-url http://localstack:4566 \
    --query 'Topics[?contains(TopicArn, `notification-topic`)].TopicArn' \
    --output text)

echo "Queue ARN: $QUEUE_ARN"
echo "Topic ARN: $TOPIC_ARN"

# Subscribe SQS queue to SNS topic
echo 'Creating subscription...'
aws sns subscribe \
    --topic-arn $TOPIC_ARN \
    --protocol sqs \
    --notification-endpoint $QUEUE_ARN \
    --endpoint-url http://localstack:4566

# Set queue policy to allow SNS to send messages
echo 'Setting queue policy...'
aws sqs set-queue-attributes \
    --queue-url http://localstack:4566/000000000000/notification-queue \
    --attributes "{\"Policy\":\"{\\\"Version\\\":\\\"2012-10-17\\\",\\\"Statement\\\":[{\\\"Effect\\\":\\\"Allow\\\",\\\"Principal\\\":{\\\"Service\\\":\\\"sns.amazonaws.com\\\"},\\\"Action\\\":\\\"sqs:SendMessage\\\",\\\"Resource\\\":\\\"$QUEUE_ARN\\\",\\\"Condition\\\":{\\\"ArnEquals\\\":{\\\"aws:SourceArn\\\":\\\"$TOPIC_ARN\\\"}}}]}\"}" \
    --endpoint-url http://localstack:4566

# Lambda function configuration
FUNCTION_NAME=PersonMessageConsumer
HANDLER=PersonMessageConsumer::PersonMessageConsumer.Function::FunctionHandler
RUNTIME=dotnet8
TIMEOUT=30
MEMORY_SIZE=256

echo 'Creating IAM role for Lambda...'
aws iam create-role \
    --role-name lambda-role \
    --assume-role-policy-document '{"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"Service":"lambda.amazonaws.com"},"Action":"sts:AssumeRole"}]}' \
    --endpoint-url http://localstack:4566 > /dev/null 2>&1 || true

echo 'Attaching basic execution policy...'
aws iam attach-role-policy \
    --role-name lambda-role \
    --policy-arn arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole \
    --endpoint-url http://localstack:4566 > /dev/null 2>&1 || true

echo 'Waiting for Lambda package...'
while [ ! -f /setup-scripts/PersonMessageConsumer.zip ]; do
    sleep 2
    echo 'Still waiting for Lambda package...'
done

echo 'Lambda package found! Deploying...'

echo 'Checking if Lambda function exists...'
if aws lambda get-function --function-name $FUNCTION_NAME --endpoint-url http://localstack:4566 > /dev/null 2>&1; then
    echo 'Lambda function exists. Updating code...'
    aws lambda update-function-code \
        --function-name $FUNCTION_NAME \
        --zip-file fileb:///setup-scripts/PersonMessageConsumer.zip \
        --endpoint-url http://localstack:4566
else
    echo 'Creating Lambda function...'
    aws lambda create-function \
        --function-name $FUNCTION_NAME \
        --runtime $RUNTIME \
        --role arn:aws:iam::000000000000:role/lambda-role \
        --handler $HANDLER \
        --zip-file fileb:///setup-scripts/PersonMessageConsumer.zip \
        --timeout $TIMEOUT \
        --memory-size $MEMORY_SIZE \
        --endpoint-url http://localstack:4566
fi

# Wait a moment for Lambda to be ready
sleep 2

echo 'Creating SQS trigger for Lambda...'
aws lambda create-event-source-mapping \
    --event-source-arn $QUEUE_ARN \
    --function-name $FUNCTION_NAME \
    --batch-size 10 \
    --endpoint-url http://localstack:4566 > /dev/null 2>&1 || true

echo 'Setup complete! Resources created:'
echo 'SQS Queue: notification-queue'
echo 'SNS Topic: notification-topic'
echo 'Subscription: SNS topic -> SQS queue'
echo 'Lambda Function: PersonMessageConsumer'
echo 'Event Source Mapping: SQS -> Lambda'

# Keep container running for debugging if needed
tail -f /dev/null
