#!/bin/bash

echo "======================================================================="
echo "           Deploying Person Message Consumer Lambda to LocalStack"
echo "======================================================================="

# Configuration
FUNCTION_NAME="PersonMessageConsumer"
HANDLER="PersonMessageConsumer::PersonMessageConsumer.Function::FunctionHandler"
RUNTIME="dotnet8"
TIMEOUT=30
MEMORY_SIZE=256
QUEUE_ARN="arn:aws:sqs:us-east-1:000000000000:notification-queue"
QUEUE_URL="http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/notification-queue"

# Set LocalStack environment
export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test
export AWS_DEFAULT_REGION=us-east-1
export AWS_ENDPOINT_URL=http://localhost:4566

echo "Building Lambda function..."
cd consumer/PersonMessageConsumer/src/PersonMessageConsumer
dotnet build

echo "Packaging Lambda function..."
dotnet lambda package

echo "Checking if Lambda function exists..."
FUNCTION_EXISTS=$(aws lambda get-function --function-name $FUNCTION_NAME --endpoint-url $AWS_ENDPOINT_URL 2>/dev/null)

if [ $? -eq 0 ]; then
    echo "Lambda function exists. Updating code..."
    aws lambda update-function-code \
        --function-name $FUNCTION_NAME \
        --zip-file fileb://bin/Debug/net8.0/PersonMessageConsumer.zip \
        --endpoint-url $AWS_ENDPOINT_URL
else
    echo "Creating Lambda function..."
    aws lambda create-function \
        --function-name $FUNCTION_NAME \
        --runtime $RUNTIME \
        --role arn:aws:iam::000000000000:role/lambda-role \
        --handler $HANDLER \
        --zip-file fileb://bin/Debug/net8.0/PersonMessageConsumer.zip \
        --timeout $TIMEOUT \
        --memory-size $MEMORY_SIZE \
        --endpoint-url $AWS_ENDPOINT_URL
fi

echo "Creating IAM role for Lambda (if not exists)..."
aws iam create-role \
    --role-name lambda-role \
    --assume-role-policy-document '{
        "Version": "2012-10-17",
        "Statement": [
            {
                "Effect": "Allow",
                "Principal": {
                    "Service": "lambda.amazonaws.com"
                },
                "Action": "sts:AssumeRole"
            }
        ]
    }' \
    --endpoint-url $AWS_ENDPOINT_URL 2>/dev/null

echo "Attaching basic execution policy..."
aws iam attach-role-policy \
    --role-name lambda-role \
    --policy-arn arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole \
    --endpoint-url $AWS_ENDPOINT_URL 2>/dev/null

echo "Creating SQS trigger for Lambda..."
aws lambda create-event-source-mapping \
    --event-source-arn $QUEUE_ARN \
    --function-name $FUNCTION_NAME \
    --batch-size 10 \
    --endpoint-url $AWS_ENDPOINT_URL 2>/dev/null

echo "======================================================================="
echo "Deployment completed!"
echo "Function Name: $FUNCTION_NAME"
echo "Handler: $HANDLER"
echo "Runtime: $RUNTIME"
echo "Queue ARN: $QUEUE_ARN"
echo "======================================================================="

echo "Testing Lambda function with a sample message..."
aws lambda invoke \
    --function-name $FUNCTION_NAME \
    --payload '{
        "Records": [
            {
                "messageId": "test-message-id",
                "body": "{\"Type\":\"Notification\",\"MessageId\":\"test-sns-id\",\"TopicArn\":\"arn:aws:sns:us-east-1:000000000000:notification-topic\",\"Subject\":\"Person Message: Test User\",\"Message\":\"{\\\"firstName\\\":\\\"Test\\\",\\\"lastName\\\":\\\"User\\\",\\\"timestamp\\\":\\\"2025-07-16T14:35:42.806382Z\\\",\\\"messageId\\\":\\\"test-person-id\\\"}\",\"MessageAttributes\":{\"MessageType\":{\"Type\":\"String\",\"Value\":\"PersonMessage\"},\"FirstName\":{\"Type\":\"String\",\"Value\":\"Test\"},\"LastName\":{\"Type\":\"String\",\"Value\":\"User\"}}}",
                "eventSource": "aws:sqs",
                "eventSourceArn": "arn:aws:sqs:us-east-1:000000000000:notification-queue",
                "awsRegion": "us-east-1"
            }
        ]
    }' \
    --endpoint-url $AWS_ENDPOINT_URL \
    output.json

echo "Lambda response:"
cat output.json
echo ""

echo "Check Lambda logs:"
aws logs describe-log-groups --endpoint-url $AWS_ENDPOINT_URL | grep PersonMessageConsumer

cd ../../../..
echo "Deployment script completed!"
