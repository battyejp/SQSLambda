#!/bin/bash

# Test script to verify SNS and SQS setup works correctly

export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test
export AWS_DEFAULT_REGION=us-east-1
export AWS_ENDPOINT_URL=http://localstack:4566

echo "=== Testing SNS to SQS Integration ==="

# Get topic ARN
TOPIC_ARN=$(aws sns list-topics \
    --endpoint-url http://localstack:4566 \
    --query 'Topics[?contains(TopicArn, `notification-topic`)].TopicArn' \
    --output text)

echo "Topic ARN: $TOPIC_ARN"

# Test 1: Publish message to SNS topic
echo "1. Publishing message to SNS topic..."
aws sns publish \
    --topic-arn $TOPIC_ARN \
    --message "Test message from SNS to SQS" \
    --subject "Test Subject" \
    --endpoint-url http://localstack:4566

echo "Message published successfully!"

# Test 2: Receive message from SQS queue
echo "2. Receiving message from SQS queue..."
aws sqs receive-message \
    --queue-url http://localstack:4566/000000000000/notification-queue \
    --endpoint-url http://localstack:4566

echo "=== Test Complete ==="

# Optional: Send multiple messages
echo "3. Sending multiple test messages..."
for i in {1..3}; do
    aws sns publish \
        --topic-arn $TOPIC_ARN \
        --message "Test message #$i" \
        --subject "Test Subject #$i" \
        --endpoint-url http://localstack:4566
    echo "Sent message #$i"
done

echo "4. Receiving all messages from queue..."
aws sqs receive-message \
    --queue-url http://localstack:4566/000000000000/notification-queue \
    --max-number-of-messages 10 \
    --endpoint-url http://localstack:4566
