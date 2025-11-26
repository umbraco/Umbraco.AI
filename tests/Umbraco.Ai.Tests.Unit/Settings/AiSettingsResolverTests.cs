using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Core.Registry;
using Umbraco.Ai.Core.Settings;
using Umbraco.Ai.Tests.Common.Fakes;

namespace Umbraco.Ai.Tests.Unit.Settings;

public class AiSettingsResolverTests
{
    private readonly Mock<IAiRegistry> _registryMock;
    private readonly AiSettingsResolver _resolver;
    private readonly IConfiguration _configuration;

    public AiSettingsResolverTests()
    {
        _registryMock = new Mock<IAiRegistry>();

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

        _resolver = new AiSettingsResolver(_registryMock.Object, _configuration);
    }

    #region ResolveSettings<TSettings> - Null handling

    [Fact]
    public void ResolveSettings_WithNullSettings_ReturnsNull()
    {
        // Arrange
        SetupProviderWithValidation("fake-provider");

        // Act
        var result = _resolver.ResolveSettings<FakeProviderSettings>("fake-provider", null);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region ResolveSettings<TSettings> - Already typed settings

    [Fact]
    public void ResolveSettings_WithAlreadyTypedSettings_ReturnsSameInstance()
    {
        // Arrange
        var settings = new FakeProviderSettings { ApiKey = "test-key" };
        SetupProviderWithValidation("fake-provider");

        // Act
        var result = _resolver.ResolveSettings<FakeProviderSettings>("fake-provider", settings);

        // Assert
        result.ShouldBeSameAs(settings);
    }

    [Fact]
    public void ResolveSettings_WithAlreadyTypedSettings_ResolvesConfigurationVariables()
    {
        // Arrange
        var settings = new FakeProviderSettings { ApiKey = "$OpenAI:ApiKey" };
        SetupProviderWithValidation("fake-provider");

        // Act
        var result = _resolver.ResolveSettings<FakeProviderSettings>("fake-provider", settings);

        // Assert
        result.ShouldNotBeNull();
        result!.ApiKey.ShouldBe("sk-test-key-from-config");
    }

    #endregion

    #region ResolveSettings<TSettings> - JsonElement deserialization

    [Fact]
    public void ResolveSettings_WithJsonElement_DeserializesCorrectly()
    {
        // Arrange
        var json = """{"ApiKey": "direct-key", "BaseUrl": "https://custom.api.com", "MaxRetries": 10}""";
        var jsonElement = JsonDocument.Parse(json).RootElement;
        SetupProviderWithValidation("fake-provider");

        // Act
        var result = _resolver.ResolveSettings<FakeProviderSettings>("fake-provider", jsonElement);

        // Assert
        result.ShouldNotBeNull();
        result!.ApiKey.ShouldBe("direct-key");
        result.BaseUrl.ShouldBe("https://custom.api.com");
        result.MaxRetries.ShouldBe(10);
    }

    [Fact]
    public void ResolveSettings_WithJsonElement_ResolvesConfigurationVariables()
    {
        // Arrange
        var json = """{"ApiKey": "$OpenAI:ApiKey", "BaseUrl": "$OpenAI:BaseUrl"}""";
        var jsonElement = JsonDocument.Parse(json).RootElement;
        SetupProviderWithValidation("fake-provider");

        // Act
        var result = _resolver.ResolveSettings<FakeProviderSettings>("fake-provider", jsonElement);

        // Assert
        result.ShouldNotBeNull();
        result!.ApiKey.ShouldBe("sk-test-key-from-config");
        result.BaseUrl.ShouldBe("https://api.openai.com");
    }

    [Fact]
    public void ResolveSettings_WithJsonElement_NonStringConfigVar_FailsDeserialization()
    {
        // Arrange - Config vars in JsonElement for non-string properties fail at JSON parse time
        // because "$TestSettings:MaxRetries" (a string) cannot be parsed as int
        var json = """{"ApiKey": "test-key", "MaxRetries": "$TestSettings:MaxRetries"}""";
        var jsonElement = JsonDocument.Parse(json).RootElement;
        SetupProviderWithValidation("fake-provider", requireApiKey: false);

        // Act
        var act = () => _resolver.ResolveSettings<FakeProviderSettings>("fake-provider", jsonElement);

        // Assert - Fails because JSON string cannot be deserialized to int
        var exception = Should.Throw<InvalidOperationException>(act);
        exception.Message.ShouldContain("Failed to deserialize JsonElement");
    }

    [Fact]
    public void ResolveSettings_ConfigVariablesOnlyWorkForStringProperties()
    {
        // Arrange - Non-string properties (int, bool) cannot hold "$ConfigVar" values
        // so config variable resolution only applies to string properties
        var settings = new FakeProviderSettings
        {
            ApiKey = "test-key",
            MaxRetries = 10,
            Enabled = true
        };
        SetupProviderWithValidation("fake-provider", requireApiKey: false);

        // Act
        var result = _resolver.ResolveSettings<FakeProviderSettings>("fake-provider", settings);

        // Assert - Non-string values pass through unchanged
        result.ShouldNotBeNull();
        result!.MaxRetries.ShouldBe(10);
        result.Enabled.ShouldBeTrue();
    }

    #endregion

    #region ResolveSettings<TSettings> - Fallback JSON serialization

    [Fact]
    public void ResolveSettings_WithAnonymousObject_FallsBackToJsonSerialization()
    {
        // Arrange
        var settings = new { ApiKey = "anon-key", BaseUrl = "https://anon.api.com" };
        SetupProviderWithValidation("fake-provider");

        // Act
        var result = _resolver.ResolveSettings<FakeProviderSettings>("fake-provider", settings);

        // Assert
        result.ShouldNotBeNull();
        result!.ApiKey.ShouldBe("anon-key");
        result.BaseUrl.ShouldBe("https://anon.api.com");
    }

    #endregion

    #region ResolveSettings<TSettings> - Configuration variable errors

    [Fact]
    public void ResolveSettings_WithMissingConfigKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new FakeProviderSettings { ApiKey = "$NonExistent:Key" };
        SetupProviderWithValidation("fake-provider", requireApiKey: false);

        // Act
        var act = () => _resolver.ResolveSettings<FakeProviderSettings>("fake-provider", settings);

        // Assert
        var exception = Should.Throw<InvalidOperationException>(act);
        exception.Message.ShouldContain("Configuration key");
        exception.Message.ShouldContain("NonExistent:Key");
        exception.Message.ShouldContain("not found");
    }

    #endregion

    #region ResolveSettings<TSettings> - Validation

    [Fact]
    public void ResolveSettings_WithMissingRequiredField_ThrowsValidationError()
    {
        // Arrange - ApiKey is required
        var settings = new FakeProviderSettings { ApiKey = null };
        SetupProviderWithValidation("fake-provider", requireApiKey: true);

        // Act
        var act = () => _resolver.ResolveSettings<FakeProviderSettings>("fake-provider", settings);

        // Assert
        var exception = Should.Throw<InvalidOperationException>(act);
        exception.Message.ShouldContain("Validation failed");
        exception.Message.ShouldContain("API Key");
        exception.Message.ShouldContain("required");
    }

    [Fact]
    public void ResolveSettings_WithValidRequiredField_PassesValidation()
    {
        // Arrange
        var settings = new FakeProviderSettings { ApiKey = "valid-key" };
        SetupProviderWithValidation("fake-provider", requireApiKey: true);

        // Act
        var result = _resolver.ResolveSettings<FakeProviderSettings>("fake-provider", settings);

        // Assert
        result.ShouldNotBeNull();
        result!.ApiKey.ShouldBe("valid-key");
    }

    #endregion

    #region ResolveSettings<TSettings> - Provider not found

    [Fact]
    public void ResolveSettings_WithUnknownProvider_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new FakeProviderSettings { ApiKey = "test" };
        _registryMock.Setup(r => r.GetProvider("unknown-provider")).Returns((IAiProvider?)null);

        // Act
        var act = () => _resolver.ResolveSettings<FakeProviderSettings>("unknown-provider", settings);

        // Assert
        var exception = Should.Throw<InvalidOperationException>(act);
        exception.Message.ShouldContain("Provider");
        exception.Message.ShouldContain("unknown-provider");
        exception.Message.ShouldContain("not found");
    }

    #endregion

    #region ResolveSettingsForProvider

    [Fact]
    public void ResolveSettingsForProvider_WithNullSettings_ReturnsNull()
    {
        // Arrange
        var provider = new FakeAiProvider().WithSettingsType<FakeProviderSettings>();

        // Act
        var result = _resolver.ResolveSettingsForProvider(provider, null);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ResolveSettingsForProvider_WithProviderWithoutSettingsType_ReturnsNull()
    {
        // Arrange
        var provider = new FakeAiProvider { SettingsType = null };

        // Act
        var result = _resolver.ResolveSettingsForProvider(provider, new { ApiKey = "test" });

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ResolveSettingsForProvider_WithValidSettings_ResolvesUsingProviderSettingsType()
    {
        // Arrange
        var provider = new FakeAiProvider("test-provider", "Test Provider")
            .WithSettingsType<FakeProviderSettings>();
        SetupProviderWithValidation("test-provider");

        var json = """{"ApiKey": "provider-key"}""";
        var jsonElement = JsonDocument.Parse(json).RootElement;

        // Act
        var result = _resolver.ResolveSettingsForProvider(provider, jsonElement);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<FakeProviderSettings>();
        ((FakeProviderSettings)result!).ApiKey.ShouldBe("provider-key");
    }

    #endregion

    #region Helper methods

    private void SetupProviderWithValidation(string providerId, bool requireApiKey = true)
    {
        var provider = new FakeAiProvider(providerId, "Test Provider");
        provider.SettingsType = typeof(FakeProviderSettings);

        var definitions = new List<AiSettingDefinition>();

        if (requireApiKey)
        {
            definitions.Add(new AiSettingDefinition
            {
                PropertyName = "ApiKey",
                Key = "api-key",
                Label = "API Key",
                Description = "Enter your API key",
                ValidationRules = [new System.ComponentModel.DataAnnotations.RequiredAttribute { ErrorMessage = "API Key is required" }]
            });
        }

        provider.SettingDefinitions = definitions;
        _registryMock.Setup(r => r.GetProvider(providerId)).Returns(provider);
    }

    #endregion
}
