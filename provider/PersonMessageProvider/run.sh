#!/bin/bash

# Set environment variables for LocalStack
export AWS_ENDPOINT_URL=http://localhost:4566
export AWS_DEFAULT_REGION=us-east-1
export TOPIC_ARN=arn:aws:sns:us-east-1:000000000000:notification-topic

# Run the application
echo "Starting Person Message Provider..."
echo "Endpoint: $AWS_ENDPOINT_URL"
echo "Region: $AWS_DEFAULT_REGION"
echo "Topic ARN: $TOPIC_ARN"
echo ""

dotnet run "$@"
