#!/bin/bash

echo "Testing Person Message Provider - Checking SQS queue for person messages..."
echo "======================================================================="

# Get messages from SQS queue
docker exec aws-setup /bin/bash -c "
aws --endpoint-url=http://localstack:4566 sqs receive-message \
    --queue-url http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/notification-queue \
    --max-number-of-messages 10 \
    --message-attribute-names All
" | jq -r '.Messages[]? | select(.Body | fromjson | .MessageAttributes?.MessageType?.Value == "PersonMessage") | .Body | fromjson | .Message | fromjson | "Person: \(.firstName) \(.lastName) (ID: \(.messageId), Time: \(.timestamp))"'

echo "======================================================================="
echo "Test completed!"
