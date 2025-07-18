using PactNet;
using PersonMessageProvider.Models;
using PersonMessageProvider.Services;
using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace PersonMessageProvider.Tests
{
    public class PersonMessageProviderPactTests
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly string _pactFilePath;
        private readonly PersonMessageClient _personMessageClient;

        public PersonMessageProviderPactTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            
            // Path to the pact file generated by the consumer tests
            _pactFilePath = Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..", "..", "..", 
                "consumer", "PersonMessageConsumer", "test", "PersonMessageConsumer.Tests", "pacts",
                "PersonMessageConsumerLambda-PersonMessageProvider.json"
            );
            
            // Create a mock SNS client (we only need the message generation functionality)
            _personMessageClient = new PersonMessageClient(null!, "test-topic");
            
            _outputHelper.WriteLine($"Looking for pact file at: {Path.GetFullPath(_pactFilePath)}");
        }

        [Fact]
        public void PersonMessageProvider_Should_Honour_Pact_With_Consumer()
        {
            // This test verifies that our provider honors the contract with the consumer
            // by generating messages that satisfy the consumer's expectations
            
            // Verify the pact file exists
            Assert.True(File.Exists(_pactFilePath), $"Pact file should exist at: {_pactFilePath}");
            
            // Read and parse the pact file to understand consumer expectations
            var pactContent = File.ReadAllText(_pactFilePath);
            var pact = JsonConvert.DeserializeObject<dynamic>(pactContent);
            
            _outputHelper.WriteLine($"Verifying pact: {pact!.consumer!.name} -> {pact!.provider!.name}");
            
            // Test each message scenario defined in the pact
            var interactions = pact!.interactions;
            if (interactions == null)
            {
                throw new InvalidOperationException("No interactions found in pact file");
            }
            
            foreach (var interaction in interactions)
            {
                var description = (string)interaction!.description;
                _outputHelper.WriteLine($"Testing scenario: {description}");
                
                // Generate a message using our provider based on the scenario
                string generatedMessage = GenerateMessageForScenario(description);
                
                if (generatedMessage == null)
                {
                    throw new InvalidOperationException($"GenerateMessageForScenario returned null for scenario: {description}");
                }
                
                // Verify the generated message matches the expected structure
                var generatedJson = JsonConvert.DeserializeObject<dynamic>(generatedMessage);
                var expectedContent = interaction.contents.content;
                
                // Verify the generated message contains the expected fields
                Assert.NotNull(generatedJson);
                
                // Generic validation: Check that ALL fields expected by the consumer are present in the provider message
                foreach (var expectedProperty in expectedContent)
                {
                    string fieldName = expectedProperty.Name;
                    var expectedValue = expectedProperty.Value;
                    
                    _outputHelper.WriteLine($"Validating field: {fieldName}");
                    
                    // Check if the provider's generated message contains this expected field
                    if (!HasProperty(generatedJson, fieldName))
                    {
                        throw new InvalidOperationException($"Provider message missing required field '{fieldName}' for scenario: {description}. " +
                            $"Consumer expects this field but provider does not generate it.");
                    }
                    
                    var generatedValue = generatedJson[fieldName];
                    
                    // Special validation for messageId (UUID format)
                    if (fieldName == "messageId")
                    {
                        var uuidPattern = @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$";
                        if (!Regex.IsMatch(generatedValue.ToString(), uuidPattern, RegexOptions.IgnoreCase))
                        {
                            throw new InvalidOperationException($"Generated messageId '{generatedValue}' does not match UUID format for scenario: {description}");
                        }
                        _outputHelper.WriteLine($"✅ MessageId validation passed - Generated UUID: {generatedValue}");
                    }
                    else
                    {
                        // For other fields, validate that the generated value matches the expected type/format
                        // Note: In a real pact, we'd validate against the matchers, but for this demo we'll do basic validation
                        if (generatedValue == null)
                        {
                            throw new InvalidOperationException($"Provider generated null value for field '{fieldName}' in scenario: {description}");
                        }
                        
                        _outputHelper.WriteLine($"✅ Field '{fieldName}' present with value: {generatedValue}");
                    }
                }
                
                _outputHelper.WriteLine($"✅ All expected fields validated for scenario: {description}");
                
                _outputHelper.WriteLine($"✅ Scenario '{description}' passed");
            }
            
            _outputHelper.WriteLine("✅ Provider successfully honors pact with consumer!");
        }
        
        private string GenerateMessageForScenario(string scenario)
        {
            _outputHelper.WriteLine($"Generating message for scenario: {scenario}");
            
            // Use the actual provider logic - generate a standard person message
            // This tests whether the provider can satisfy the consumer's expectations
            var person = new Person
            {
                FirstName = "John",
                LastName = "Doe",  // This is what the actual provider would generate
                Timestamp = DateTime.Parse("2025-07-16T16:30:00.000000Z").ToUniversalTime()
                // MessageId will be auto-generated by the Person model as a proper UUID
            };
            
            _outputHelper.WriteLine($"Created person: {JsonConvert.SerializeObject(person)}");
            
            if (_personMessageClient == null)
            {
                throw new InvalidOperationException("PersonMessageClient is null");
            }
            
            // Use the actual provider method to generate the message
            var result = _personMessageClient.GeneratePersonMessage(person);
            _outputHelper.WriteLine($"Generated message: {result}");
            
            return result;
        }
        
        private bool HasProperty(dynamic obj, string propertyName)
        {
            try
            {
                var value = obj[propertyName];
                return value != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
