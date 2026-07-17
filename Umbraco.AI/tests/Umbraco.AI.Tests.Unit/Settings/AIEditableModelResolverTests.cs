using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
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
            { "TestSettings:MaxRetries", "5" },
            { "Umbraco:AI:Secrets:ApiKey", "sk-secret-from-config" },
            { "Umbraco:AI:Variables:BaseUrl", "https://env.example.com" },
            // A genuine secret that must stay unreachable from settings.
            { "ConnectionStrings:umbracoDbDSN", "Server=.;Database=secret;User=sa;Password=hunter2" },
            // Sits just outside the Umbraco:AI:Secrets prefix boundary.
            { "Umbraco:AI:SecretsBackup:Token", "should-not-resolve" },
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    // Existing mechanics tests reference $OpenAI:* and $TestSettings:* keys, so the default
    // helper allows those prefixes. Pass explicit prefixes to exercise the allow-list itself.
    private AIEditableModelResolver CreateResolver(params string[] allowedPrefixes)
    {
        var prefixes = allowedPrefixes.Length > 0
            ? allowedPrefixes
            : ["OpenAI", "TestSettings"];

        var options = Options.Create(new AIOptions { AllowedConfigurationKeyPrefixes = prefixes });
        return new AIEditableModelResolver(_configuration, options);
    }

    // Resolver wired with the production defaults (Umbraco:AI:Secrets / Umbraco:AI:Variables).
    private AIEditableModelResolver CreateDefaultResolver()
        => new(_configuration, Options.Create(new AIOptions()));

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
        // Arrange - Key is under an allowed prefix but absent from configuration, so it must
        // reach the "not found" path rather than the allow-list rejection.
        var settings = new FakeProviderSettings { ApiKey = "$OpenAI:NonExistentKey" };
        var resolver = CreateResolver();

        // Act
        var act = () => resolver.ResolveModel<FakeProviderSettings>(settings);

        // Assert
        var exception = Should.Throw<InvalidOperationException>(act);
        exception.Message.ShouldContain("Configuration key");
        exception.Message.ShouldContain("OpenAI:NonExistentKey");
        exception.Message.ShouldContain("not found");
    }

    #endregion

    #region ResolveModel<TModel> - Configuration key allow-list

    [Fact]
    public void ResolveModel_WithConfigKeyOutsideAllowedPrefix_Throws()
    {
        // A sensitive key outside the allow-list (here a connection string) referenced from
        // a settings field. It exists in configuration but must stay unreachable.
        var settings = new FakeProviderSettings { ApiKey = "$ConnectionStrings:umbracoDbDSN" };
        var resolver = CreateResolver(); // allows OpenAI/TestSettings only

        var act = () => resolver.ResolveModel<FakeProviderSettings>(settings);

        var exception = Should.Throw<InvalidOperationException>(act);
        exception.Message.ShouldContain("ConnectionStrings:umbracoDbDSN");
        exception.Message.ShouldContain("not permitted");
        // The secret value itself must never leak into the error.
        exception.Message.ShouldNotContain("hunter2");
    }

    [Fact]
    public void ResolveModel_WithConfigKeyUnderAllowedPrefix_Resolves()
    {
        // Secret key referenced from a sensitive field - the sanctioned use.
        var settings = new FakeProviderSettings { SecretField = "$Umbraco:AI:Secrets:ApiKey" };
        var resolver = CreateResolver("Umbraco:AI:Secrets");

        var result = resolver.ResolveModel<FakeProviderSettings>(settings);

        result.ShouldNotBeNull();
        result!.SecretField.ShouldBe("sk-secret-from-config");
    }

    [Fact]
    public void ResolveModel_AllowedPrefixMatchingIsSegmentAware_Throws()
    {
        // Umbraco:AI:SecretsBackup must NOT be admitted by the Umbraco:AI:Secrets prefix -
        // the boundary is the ':' segment separator.
        var settings = new FakeProviderSettings { ApiKey = "$Umbraco:AI:SecretsBackup:Token" };
        var resolver = CreateResolver("Umbraco:AI:Secrets");

        var act = () => resolver.ResolveModel<FakeProviderSettings>(settings);

        var exception = Should.Throw<InvalidOperationException>(act);
        exception.Message.ShouldContain("not permitted");
    }

    [Fact]
    public void ResolveModel_WithDefaultOptions_RejectsKeysOutsideAllowedSections()
    {
        // Secure by default: with the production defaults the only resolvable sections are
        // Umbraco:AI:Secrets and Umbraco:AI:Variables, so an arbitrary key is rejected.
        var settings = new FakeProviderSettings { ApiKey = "$OpenAI:ApiKey" };
        var resolver = CreateDefaultResolver();

        var act = () => resolver.ResolveModel<FakeProviderSettings>(settings);

        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("not permitted");
    }

    [Fact]
    public void ResolveModel_WithDefaultOptions_ResolvesSecretsAndVariablesSections()
    {
        // Both default sections resolve out of the box: a Secret into a sensitive field, a
        // Variable into an ordinary field.
        var settings = new FakeProviderSettings
        {
            SecretField = "$Umbraco:AI:Secrets:ApiKey",
            BaseUrl = "$Umbraco:AI:Variables:BaseUrl",
        };
        var resolver = CreateDefaultResolver();

        var result = resolver.ResolveModel<FakeProviderSettings>(settings);

        result.ShouldNotBeNull();
        result!.SecretField.ShouldBe("sk-secret-from-config");
        result.BaseUrl.ShouldBe("https://env.example.com");
    }

    #endregion

    #region ResolveModel<TModel> - Secret keys restricted to sensitive fields

    [Fact]
    public void ResolveModel_SecretKeyInNonSensitiveField_Throws()
    {
        // A secret key must not resolve into a non-sensitive field; it is only allowed in
        // fields the system treats as credential-bearing. Blocked.
        var settings = new FakeProviderSettings { ApiKey = "$Umbraco:AI:Secrets:ApiKey" };
        var resolver = CreateDefaultResolver();

        var act = () => resolver.ResolveModel<FakeProviderSettings>(settings);

        var exception = Should.Throw<InvalidOperationException>(act);
        exception.Message.ShouldContain("secret");
        exception.Message.ShouldContain("sensitive field");
        exception.Message.ShouldNotContain("sk-secret-from-config");
    }

    [Fact]
    public void ResolveModel_SecretKeyInSensitiveField_Resolves()
    {
        var settings = new FakeProviderSettings { SecretField = "$Umbraco:AI:Secrets:ApiKey" };
        var resolver = CreateDefaultResolver();

        var result = resolver.ResolveModel<FakeProviderSettings>(settings);

        result.ShouldNotBeNull();
        result!.SecretField.ShouldBe("sk-secret-from-config");
    }

    [Fact]
    public void ResolveModel_VariablesKeyInNonSensitiveField_Resolves()
    {
        // Variables are not secret, so they are unrestricted by field sensitivity.
        var settings = new FakeProviderSettings { BaseUrl = "$Umbraco:AI:Variables:BaseUrl" };
        var resolver = CreateDefaultResolver();

        var result = resolver.ResolveModel<FakeProviderSettings>(settings);

        result.ShouldNotBeNull();
        result!.BaseUrl.ShouldBe("https://env.example.com");
    }

    #endregion

    #region ResolveModel<TModel> - Literal $ escaping ($$)

    [Fact]
    public void ResolveModel_LeadingDoubleDollar_ResolvesToLiteralSingleDollar()
    {
        // A value that must start with a literal '$' (e.g. a guardrail regex/contains pattern)
        // is escaped with a leading '$$'. It is returned verbatim minus one '$' — never treated
        // as a config reference, so the allow-list is not consulted.
        var settings = new FakeProviderSettings { BaseUrl = "$$ConnectionStrings:umbracoDbDSN" };
        var resolver = CreateDefaultResolver();

        var result = resolver.ResolveModel<FakeProviderSettings>(settings);

        result.ShouldNotBeNull();
        result!.BaseUrl.ShouldBe("$ConnectionStrings:umbracoDbDSN");
    }

    [Fact]
    public void ResolveModel_LoneDoubleDollar_ResolvesToSingleDollar()
    {
        var settings = new FakeProviderSettings { BaseUrl = "$$" };
        var resolver = CreateDefaultResolver();

        var result = resolver.ResolveModel<FakeProviderSettings>(settings);

        result.ShouldNotBeNull();
        result!.BaseUrl.ShouldBe("$");
    }

    [Fact]
    public void ResolveModel_TrailingDollar_IsLeftUnchanged()
    {
        // A trailing '$' (e.g. a regex end-of-line anchor) does not start with '$', so it is
        // never treated as a reference and needs no escaping — the common, important case.
        var settings = new FakeProviderSettings { BaseUrl = @"^\d+$" };
        var resolver = CreateDefaultResolver();

        var result = resolver.ResolveModel<FakeProviderSettings>(settings);

        result.ShouldNotBeNull();
        result!.BaseUrl.ShouldBe(@"^\d+$");
    }

    #endregion

    #region EscapeLiteral

    [Fact]
    public void EscapeLiteral_Null_ReturnsNull()
    {
        CreateDefaultResolver().EscapeLiteral(null).ShouldBeNull();
    }

    [Fact]
    public void EscapeLiteral_ValueWithoutLeadingDollar_ReturnedUnchanged()
    {
        CreateDefaultResolver().EscapeLiteral("# Heading").ShouldBe("# Heading");
    }

    [Fact]
    public void EscapeLiteral_ValueWithLeadingDollar_IsPrefixedWithDollar()
    {
        CreateDefaultResolver().EscapeLiteral("$x").ShouldBe("$$x");
    }

    [Fact]
    public void EscapeLiteral_ValueWithDoubleLeadingDollar_GainsOneMoreDollar()
    {
        CreateDefaultResolver().EscapeLiteral("$$x$$").ShouldBe("$$$x$$");
    }

    [Fact]
    public void EscapeLiteral_ThenResolveModel_RoundTripsToOriginalLiteral()
    {
        // A literal that begins with '$' must survive a store-then-resolve round trip verbatim,
        // rather than being treated as (or rejected as) a configuration reference.
        const string literal = "$5 per month";
        var resolver = CreateDefaultResolver();

        var escaped = resolver.EscapeLiteral(literal);
        var settings = new FakeProviderSettings { BaseUrl = escaped };

        var result = resolver.ResolveModel<FakeProviderSettings>(settings);

        result.ShouldNotBeNull();
        result!.BaseUrl.ShouldBe(literal);
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
