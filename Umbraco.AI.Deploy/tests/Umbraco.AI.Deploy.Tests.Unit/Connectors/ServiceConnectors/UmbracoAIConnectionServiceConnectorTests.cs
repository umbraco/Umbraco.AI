using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Deploy.Artifacts;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Deploy.Connectors.ServiceConnectors;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Xunit;

namespace Umbraco.AI.Deploy.Tests.Unit.Connectors.ServiceConnectors;

public class UmbracoAIConnectionServiceConnectorTests
{
    private const string ProviderId = "test-provider";

    private readonly Mock<IAIConnectionService> _connectionServiceMock;
    private readonly Mock<UmbracoAIDeploySettingsAccessor> _settingsAccessorMock;
    private readonly UmbracoAIDeployConnectionSettings _connectionSettings = new();
    private readonly UmbracoAIConnectionServiceConnector _connector;

    public UmbracoAIConnectionServiceConnectorTests()
    {
        _connectionServiceMock = new Mock<IAIConnectionService>();
        _connectionServiceMock
            .Setup(x => x.SaveConnectionAsync(It.IsAny<AIConnection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConnection c, CancellationToken _) => c);

        _settingsAccessorMock = new Mock<UmbracoAIDeploySettingsAccessor>(MockBehavior.Strict, null!);
        _settingsAccessorMock.Setup(x => x.Settings).Returns(new UmbracoAIDeploySettings { Connections = _connectionSettings });

        // Mirrors a typical provider: a required API key, an optional secret and a plain endpoint.
        var schema = new AIEditableModelSchema(typeof(object),
        [
            new AIEditableModelField { Key = "apiKey", Label = "API Key", PropertyName = "ApiKey", IsSensitive = true, ValidationRules = [new RequiredAttribute()] },
            new AIEditableModelField { Key = "organizationId", Label = "Organization", PropertyName = "OrganizationId", IsSensitive = true },
            new AIEditableModelField { Key = "endpoint", Label = "Endpoint", PropertyName = "Endpoint" },
        ]);
        var provider = new Mock<IAIProvider>();
        provider.SetupGet(x => x.Id).Returns(ProviderId);
        provider.Setup(x => x.GetSettingsSchema()).Returns(schema);

        _connector = new UmbracoAIConnectionServiceConnector(
            _connectionServiceMock.Object,
            _settingsAccessorMock.Object,
            new AIProviderCollection(() => [provider.Object]),
            NullLogger<UmbracoAIConnectionServiceConnector>.Instance);
    }

    // ---- Export ----

    [Fact]
    public async Task GetArtifactAsync_SensitiveLiteral_IsNotWritten()
    {
        // The shape a connection has at runtime: decrypted JSON with camelCase keys.
        var artifact = await ExportAsync(Json(new { apiKey = "sk-live-secret", endpoint = "https://api.example.com" }));

        var settings = ReadSettings(artifact);
        settings.ShouldNotContainKey("apiKey");
        settings["endpoint"].GetString().ShouldBe("https://api.example.com");
    }

    [Fact]
    public async Task GetArtifactAsync_SensitiveLiteral_IsNotWritten_WhateverTheKeyCasing()
    {
        var artifact = await ExportAsync(new Dictionary<string, object?> { ["ApiKey"] = "sk-live-secret" });

        ReadSettings(artifact).ShouldNotContainKey("ApiKey");
    }

    [Fact]
    public async Task GetArtifactAsync_SensitiveLiteral_IsNotWritten_WithObsoleteOptionsTurnedOff()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        _connectionSettings.IgnoreEncrypted = false;
        _connectionSettings.IgnoreSensitive = false;
#pragma warning restore CS0618

        var artifact = await ExportAsync(Json(new { apiKey = "sk-live-secret", organizationId = "org-secret" }));

        var settings = ReadSettings(artifact);
        settings.ShouldNotContainKey("apiKey");
        settings.ShouldNotContainKey("organizationId");
    }

    [Fact]
    public async Task GetArtifactAsync_ConfigurationReference_IsWritten()
    {
        var artifact = await ExportAsync(Json(new { apiKey = "$Umbraco:AI:Secrets:OpenAI" }));

        ReadSettings(artifact)["apiKey"].GetString().ShouldBe("$Umbraco:AI:Secrets:OpenAI");
    }

    [Fact]
    public async Task GetArtifactAsync_EscapedLiteral_IsNotWritten()
    {
        var artifact = await ExportAsync(Json(new { apiKey = "$$starts-with-dollar" }));

        ReadSettings(artifact).ShouldNotContainKey("apiKey");
    }

    [Fact]
    public async Task GetArtifactAsync_EncryptedValue_IsNotWritten_EvenInANonSensitiveField()
    {
        var artifact = await ExportAsync(Json(new { endpoint = "ENC:abc123" }));

        ReadSettings(artifact).ShouldNotContainKey("endpoint");
    }

    [Fact]
    public async Task GetArtifactAsync_IncludedSensitiveSetting_IsWritten_CaseInsensitively()
    {
        _connectionSettings.IncludeSensitiveSettings = ["ApiKey"];

        var artifact = await ExportAsync(Json(new { apiKey = "sk-live-secret" }));

        ReadSettings(artifact)["apiKey"].GetString().ShouldBe("sk-live-secret");
    }

    [Fact]
    public async Task GetArtifactAsync_IgnoredSetting_IsNotWritten_CaseInsensitively()
    {
        _connectionSettings.IgnoreSettings = ["Endpoint"];

        var artifact = await ExportAsync(Json(new { endpoint = "https://api.example.com" }));

        ReadSettings(artifact).ShouldNotContainKey("endpoint");
    }

    [Fact]
    public async Task GetArtifactAsync_IgnoredSetting_WinsOverIncludedSensitiveSetting()
    {
        _connectionSettings.IgnoreSettings = ["apiKey"];
        _connectionSettings.IncludeSensitiveSettings = ["apiKey"];

        var artifact = await ExportAsync(Json(new { apiKey = "$Umbraco:AI:Secrets:OpenAI" }));

        ReadSettings(artifact).ShouldNotContainKey("apiKey");
    }

    [Fact]
    public async Task GetArtifactAsync_UnknownProvider_TreatsEveryStringAsSensitive()
    {
        var connection = new AIConnection
        {
            Alias = "test-connection",
            Name = "Test Connection",
            ProviderId = "not-installed",
            Settings = Json(new { anything = "sk-live-secret", reference = "$Umbraco:AI:Secrets:X", maxTokens = 100 })
        };

        var artifact = await _connector.GetArtifactAsync(new GuidUdi("umbraco-ai-connection", connection.Id), connection);

        var settings = ReadSettings(artifact);
        settings.ShouldNotContainKey("anything");
        settings["reference"].GetString().ShouldBe("$Umbraco:AI:Secrets:X");
        settings["maxTokens"].GetInt32().ShouldBe(100);
    }

    [Fact]
    public async Task GetArtifactAsync_WithNullSettings_ReturnsNullSettings()
    {
        var artifact = await ExportAsync(null);

        artifact.Settings.ShouldBeNull();
    }

    // ---- Import ----

    [Fact]
    public async Task ProcessAsync_ArtifactCarryingASensitiveLiteral_DoesNotImportIt()
    {
        // An artifact written by an affected version still holds the key in plain text.
        var existing = NewConnection(Json(new { apiKey = "target-own-key" }));

        var saved = await ImportAsync(Json(new { apiKey = "leaked-source-key", endpoint = "https://source" }), existing);

        var settings = SavedSettings(saved);
        settings["apiKey"].GetString().ShouldBe("target-own-key");
        settings["endpoint"].GetString().ShouldBe("https://source");
    }

    [Fact]
    public async Task ProcessAsync_IncludedSensitiveSetting_IsImported()
    {
        _connectionSettings.IncludeSensitiveSettings = ["apiKey"];
        var existing = NewConnection(Json(new { apiKey = "target-own-key" }));

        var saved = await ImportAsync(Json(new { apiKey = "source-key" }), existing);

        SavedSettings(saved)["apiKey"].GetString().ShouldBe("source-key");
    }

    [Fact]
    public async Task ProcessAsync_ConfigurationReference_ReplacesTargetValue()
    {
        var existing = NewConnection(Json(new { apiKey = "target-own-key" }));

        var saved = await ImportAsync(Json(new { apiKey = "$Umbraco:AI:Secrets:OpenAI" }), existing);

        SavedSettings(saved)["apiKey"].GetString().ShouldBe("$Umbraco:AI:Secrets:OpenAI");
        saved.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessAsync_ExistingConnection_KeepsItsOwnSecrets()
    {
        var existing = NewConnection(Json(new { apiKey = "target-own-key", organizationId = "target-org" }));

        var saved = await ImportAsync(Json(new { endpoint = "https://source" }), existing);

        var settings = SavedSettings(saved);
        settings["apiKey"].GetString().ShouldBe("target-own-key");
        settings["organizationId"].GetString().ShouldBe("target-org");
        saved.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessAsync_NewConnection_MissingRequiredSecret_IsImportedInactiveWithPlaceholder()
    {
        var saved = await ImportAsync(Json(new { endpoint = "https://source" }), existing: null);

        SavedSettings(saved)["apiKey"].GetString().ShouldBe(UmbracoAIConnectionServiceConnector.MissingSecretPlaceholder);
        saved.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_NewConnection_MissingOptionalSecret_IsLeftEmpty()
    {
        var saved = await ImportAsync(Json(new { apiKey = "$Umbraco:AI:Secrets:OpenAI" }), existing: null);

        SavedSettings(saved).ShouldNotContainKey("organizationId");
        saved.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessAsync_Redeploy_KeepsConnectionInactiveWhilePlaceholderIsInPlace()
    {
        var existing = NewConnection(Json(new { apiKey = UmbracoAIConnectionServiceConnector.MissingSecretPlaceholder }));
        existing.IsActive = false;

        var saved = await ImportAsync(Json(new { endpoint = "https://source" }), existing, sourceIsActive: true);

        saved.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_Redeploy_AfterRealSecretIsSet_FollowsSourceActiveState()
    {
        var existing = NewConnection(Json(new { apiKey = "target-own-key" }));

        var saved = await ImportAsync(Json(new { endpoint = "https://source" }), existing, sourceIsActive: true);

        saved.IsActive.ShouldBeTrue();
    }

    // ---- Other ----

    [Fact]
    public async Task GetEntityAsync_ReturnsConnection()
    {
        var connectionId = Guid.NewGuid();
        var connection = NewConnection(null);
        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        var result = await _connector.GetEntityAsync(connectionId);

        result.ShouldBe(connection);
    }

    [Fact]
    public void GetEntityName_ReturnsConnectionName()
    {
        _connector.GetEntityName(NewConnection(null)).ShouldBe("Test Connection");
    }

    private static AIConnection NewConnection(object? settings) => new()
    {
        Alias = "test-connection",
        Name = "Test Connection",
        ProviderId = ProviderId,
        Settings = settings
    };

    private static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value);

    private async Task<AIConnectionArtifact> ExportAsync(object? settings)
    {
        var connection = NewConnection(settings);
        var artifact = await _connector.GetArtifactAsync(new GuidUdi("umbraco-ai-connection", connection.Id), connection);
        artifact.ShouldNotBeNull();
        return artifact;
    }

    private async Task<AIConnection> ImportAsync(JsonElement artifactSettings, AIConnection? existing, bool sourceIsActive = true)
    {
        var id = existing?.Id ?? Guid.NewGuid();
        var artifact = new AIConnectionArtifact(new GuidUdi("umbraco-ai-connection", id))
        {
            Alias = "test-connection",
            Name = "Test Connection",
            ProviderId = ProviderId,
            Settings = artifactSettings,
            IsActive = sourceIsActive
        };

        var state = new ArtifactDeployState<AIConnectionArtifact, AIConnection>(artifact, existing, _connector, 1);
        await _connector.ProcessAsync(state, Mock.Of<IDeployContext>(), 1);

        state.Entity.ShouldNotBeNull();
        return state.Entity;
    }

    private static Dictionary<string, JsonElement> ReadSettings(AIConnectionArtifact? artifact)
    {
        artifact.ShouldNotBeNull();
        artifact.Settings.ShouldNotBeNull();
        return new Dictionary<string, JsonElement>(
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(artifact.Settings.Value)!,
            StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, JsonElement> SavedSettings(AIConnection connection)
        => new(JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(connection.Settings))!,
            StringComparer.OrdinalIgnoreCase);
}
