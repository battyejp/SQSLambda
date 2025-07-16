@echo off
REM Set environment variables for LocalStack
set AWS_ENDPOINT_URL=http://localhost:4566
set AWS_DEFAULT_REGION=us-east-1
set TOPIC_ARN=arn:aws:sns:us-east-1:000000000000:notification-topic

REM Run the application
echo Starting Person Message Provider...
echo Endpoint: %AWS_ENDPOINT_URL%
echo Region: %AWS_DEFAULT_REGION%
echo Topic ARN: %TOPIC_ARN%
echo.

dotnet run %*
