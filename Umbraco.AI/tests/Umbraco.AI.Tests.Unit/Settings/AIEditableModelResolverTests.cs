using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Settings;

public class AIEditableModelResolverTests
{
    private readonly IConfiguration _configuration;

    public AIEditableModelResolverTests()
    {
        var configData = new Dictionary<string, string?>
        {
            { "OpenAI:ApiKey", "sk-test-key-from-config" },
            { "OpenAI:BaseUrl", "https://api.openai.com" },
            { "TestSettings:Enabled", "true" },
            { "TestSettings:MaxRetries", "5" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    private AIEditableModelResolver CreateResolver()
    {
        return new AIEditableModelResolver(_configuration);
    }

    private static AIEditableModelSchema CreateSchema(bool requireApiKey = true)
    {
        var fields = new List<AIEditableModelField>();

        if (requireApiKey)
        {
            fields.Add(new AIEditableModelField
            {
                PropertyName = "ApiKey",
                Key = "api-key",
                Label = "API Key",
                Description = "Enter your API key",
                ValidationRules = [new System.ComponentModel.DataAnnotations.RequiredAttribute { ErrorMessage = "API Key is required" }]
            });
        }

        return new AIEditableModelSchema(typeof(FakeProviderSettings), fields);
    }

    #region ResolveModel<TModel> - Null handling

    [Fact]
    public void ResolveModel_WithNullData_ReturnsNull()
    {
        // Arrange
        var resolver = CreateResolver();

        // Act
        var result = resolver.ResolveModel<FakeProviderSettings>(null);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region ResolveModel<TModel> - Already typed data

    [Fact]
    public void ResolveModel_WithAlreadyTypedData_ReturnsNewInstance()
    {
        // Arrange
        var settings = new FakeProviderSettings { ApiKey = "test-key" };
        var resolver = CreateResolver();

        // Act
        var result = resolver.ResolveModel<FakeProviderSettings>(settings);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeSameAs(settings);
        result!.ApiKey.ShouldBe("test-key");
    }

    [Fact]
    public void ResolveModel_WithAlreadyTypedData_DoesNotMutateOriginal()
    {
        // Arrange
        var settings = new FakeProviderSettings { ApiKey = "$OpenAI:ApiKey" };
        var resolver = CreateResolver();

        // Act
        var result = resolver.ResolveModel<FakeProviderSettings>(settings);

        // Assert - original should still have the config reference
        settings.ApiKey.ShouldBe("$OpenAI:ApiKey");
        // resolved copy should have the actual value
        result.ShouldNotBeNull();
        result!.ApiKey.ShouldBe("sk-test-key-from-config");
    }

    [Fact]
    public void ResolveModel_WithAlreadyTypedData_ResolvesConfigurationVariables()
    {
        // Arrange
        var settings = new FakeProviderSettings { ApiKey = "$OpenAI:ApiKey" };
        var resolver = CreateResolver();

        // Act
        var result = resolver.ResolveModel<FakeProviderSettings>(settings);

        // Assert
        result.ShouldNotBeNull();
        result!.ApiKey.ShouldBe("sk-test-key-from-config");
    }

    #endregion

    #region ResolveModel<TModel> - JsonElement deserialization

    [Fact]
    public void ResolveModel_WithJsonElement_DeserializesCorrectly()
    {
        // Arrange - JSON uses camelCase to match the JsonNamingPolicy.CamelCase in AIEditableModelResolver
        var json = """{"apiKey": "direct-key", "baseUrl": "https://custom.api.com", "maxRetries": 10}""";
        var jsonElement = JsonDocument.Parse(json).RootElement;
        var resolver = CreateResolver();

        // Act
        var result = resolver.ResolveModel<FakeProviderSettings>(jsonElement);

        // Assert
        result.ShouldNotBeNull();
        result!.ApiKey.ShouldBe("direct-key");
        result.BaseUrl.ShouldBe("https://custom.api.com");
        result.MaxRetries.ShouldBe(10);
    }

    [Fact]
    public void ResolveModel_WithJsonElement_ResolvesConfigurationVariables()
    {
        // Arrange - JSON uses camelCase to match the JsonNamingPolicy.CamelCase in AIEditableModelResolver
        var json = """{"apiKey": "$OpenAI:ApiKey", "baseUrl": "$OpenAI:BaseUrl"}""";
        var jsonElement = JsonDocument.Parse(json).RootElement;
        var resolver = CreateResolver();

        // Act
        var result = resolver.ResolveModel<FakeProviderSettings>(jsonElement);

        // Assert
        result.ShouldNotBeNull();
        result!.ApiKey.ShouldBe("sk-test-key-from-config");
        result.BaseUrl.ShouldBe("https://api.openai.com");
    }

    [Fact]
    public void ResolveModel_WithJsonElement_NonStringConfigVar_FailsDeserialization()
    {
        // Arrange - Config vars in JsonElement for non-string properties fail at JSON parse time
        // because "$TestSettings:MaxRetries" (a string) cannot be parsed as int
        // JSON uses camelCase to match the JsonNamingPolicy.CamelCase in AIEditableModelResolver
        var json = """{"apiKey": "test-key", "maxRetries": "$TestSettings:MaxRetries"}""";
        var jsonElement = JsonDocument.Parse(json).RootElement;
        var resolver = CreateResolver();

        // Act
        var act = () => resolver.ResolveModel<FakeProviderSettings>(jsonElement);

        // Assert - Fails because JSON string cannot be deserialized to int
        // JsonException is thrown directly from JsonSerializer.Deserialize
        var exception = Should.Throw<JsonException>(act);
        exception.Message.ShouldContain("maxRetries");
    }

    [Fact]
    public void ResolveModel_ConfigVariablesOnlyWorkForStringProperties()
    {
        // Arrange - Non-string properties (int, bool) cannot hold "$ConfigVar" values
        // so config variable resolution only applies to string properties
        var settings = new FakeProviderSettings
        {
            ApiKey = "test-key",
            MaxRetries = 10,
            Enabled = true
        };
        var resolver = CreateResolver();

        // Act
        var result = resolver.ResolveModel<FakeProviderSettings>(settings);

        // Assert - Non-string values pass through unchanged
        result.ShouldNotBeNull();
        result!.MaxRetries.ShouldBe(10);
        result.Enabled.ShouldBeTrue();
    }

    #endregion

    #region ResolveModel<TModel> - Fallback JSON serialization

    [Fact]
    public void ResolveModel_WithAnonymousObject_FallsBackToJsonSerialization()
    {
        // Arrange
        var settings = new { ApiKey = "anon-key", BaseUrl = "https://anon.api.com" };
        var resolver = CreateResolver();

        // Act
        var result = resolver.ResolveModel<FakeProviderSettings>(settings);

        // Assert
        result.ShouldNotBeNull();
        result!.ApiKey.ShouldBe("anon-key");
        result.BaseUrl.ShouldBe("https://anon.api.com");
    }

    #endregion

    #region ResolveModel<TModel> - Configuration variable errors

    [Fact]
    public void ResolveModel_WithMissingConfigKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new FakeProviderSettings { ApiKey = "$NonExistent:Key" };
        var resolver = CreateResolver();

        // Act
        var act = () => resolver.ResolveModel<FakeProviderSettings>(settings);

        // Assert
        var exception = Should.Throw<InvalidOperationException>(act);
        exception.Message.ShouldContain("Configuration key");
        exception.Message.ShouldContain("NonExistent:Key");
        exception.Message.ShouldContain("not found");
    }

    #endregion

    #region ResolveModel<TModel> - Validation

    [Fact]
    public void ResolveModel_WithMissingRequiredField_ThrowsValidationError()
    {
        // Arrange - ApiKey is required
        var settings = new FakeProviderSettings { ApiKey = null };
        var schema = CreateSchema(requireApiKey: true);
        var resolver = CreateResolver();

        // Act
        var act = () => resolver.ResolveModel<FakeProviderSettings>(settings, schema);

        // Assert
        var exception = Should.Throw<InvalidOperationException>(act);
        exception.Message.ShouldContain("Validation failed");
        exception.Message.ShouldContain("API Key");
        exception.Message.ShouldContain("required");
    }

    [Fact]
    public void ResolveModel_WithValidRequiredField_PassesValidation()
    {
        // Arrange
        var settings = new FakeProviderSettings { ApiKey = "valid-key" };
        var schema = CreateSchema(requireApiKey: true);
        var resolver = CreateResolver();

        // Act
        var result = resolver.ResolveModel<FakeProviderSettings>(settings, schema);

        // Assert
        result.ShouldNotBeNull();
        result!.ApiKey.ShouldBe("valid-key");
    }

    #endregion

    #region ResolveModel<TModel> - No schema (validation skip)

    [Fact]
    public void ResolveModel_WithNoSchema_SkipsValidation()
    {
        // Arrange
        var settings = new FakeProviderSettings { ApiKey = "test" };
        var resolver = CreateResolver();

        // Act - Resolves successfully because no schema means no validation
        var result = resolver.ResolveModel<FakeProviderSettings>(settings);

        // Assert
        result.ShouldNotBeNull();
        result!.ApiKey.ShouldBe("test");
    }

    #endregion

    #region ResolveSettingsForProvider

    [Fact]
    public void ResolveSettingsForProvider_WithNullSettings_ReturnsNull()
    {
        // Arrange
        var provider = new FakeAIProvider().WithSettingsType<FakeProviderSettings>();
        var resolver = CreateResolver();

        // Act
        var result = resolver.ResolveSettingsForProvider(provider, null);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ResolveSettingsForProvider_WithProviderWithoutSettingsType_ReturnsNull()
    {
        // Arrange
        var provider = new FakeAIProvider { SettingsType = null };
        var resolver = CreateResolver();

        // Act
        var result = resolver.ResolveSettingsForProvider(provider, new { ApiKey = "test" });

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ResolveSettingsForProvider_WithValidSettings_ResolvesUsingProviderSettingsType()
    {
        // Arrange
        var provider = new FakeAIProvider("test-provider", "Test Provider")
            .WithSettingsType<FakeProviderSettings>();
        provider.SettingsSchema = CreateSchema(requireApiKey: false);
        var resolver = CreateResolver();

        // JSON uses camelCase to match the JsonNamingPolicy.CamelCase in AIEditableModelResolver
        var json = """{"apiKey": "provider-key"}""";
        var jsonElement = JsonDocument.Parse(json).RootElement;

        // Act
        var result = resolver.ResolveSettingsForProvider(provider, jsonElement);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<FakeProviderSettings>();
        ((FakeProviderSettings)result!).ApiKey.ShouldBe("provider-key");
    }

    #endregion
}
