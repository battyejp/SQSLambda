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

echo 'Setup complete! Resources created:'
echo 'SQS Queue: notification-queue'
echo 'SNS Topic: notification-topic'
echo 'Subscription: SNS topic -> SQS queue'

# Keep container running for debugging if needed
tail -f /dev/null
