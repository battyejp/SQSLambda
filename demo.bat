@echo off
echo ======================================================================
echo                    SQS Lambda LocalStack Demo
echo ======================================================================
echo.
echo This demo shows the complete flow:
echo 1. Start LocalStack services
echo 2. Send person messages using .NET console app
echo 3. Verify messages in SQS queue
echo.
echo Starting LocalStack services...
docker-compose up -d
echo.
echo Waiting for services to initialize...
timeout /t 10 /nobreak > nul
echo.
echo Sending person messages...
echo.

cd provider\PersonMessageProvider

echo Sending message for Alice Johnson...
call run.bat "Alice" "Johnson"
echo.

echo Sending message for Bob Smith...  
call run.bat "Bob" "Smith"
echo.

echo Sending message for Charlie Brown...
call run.bat "Charlie" "Brown"
echo.

cd ..\..

echo ======================================================================
echo Checking messages in SQS queue...
echo ======================================================================
docker exec aws-setup /bin/bash -c "aws --endpoint-url=http://localstack:4566 sqs receive-message --queue-url http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/notification-queue --max-number-of-messages 10" > temp_messages.json

echo Found the following messages in the queue:
findstr /C:"PersonMessage" temp_messages.json
del temp_messages.json

echo.
echo ======================================================================
echo Demo completed successfully!
echo ======================================================================
echo.
echo Next steps:
echo - Build a consumer application in the consumer/ folder
echo - Add Lambda functions to process the messages
echo - Implement message filtering based on attributes
echo.
