using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Deploy.Artifacts;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Deploy;

namespace Umbraco.AI.Deploy.Connectors.ServiceConnectors;

/// <summary>
/// Service connector for Umbraco AI Connections, responsible for synchronizing AIConnection entities with their
/// corresponding AIConnectionArtifact during deploy operations. This connector handles retrieval, artifact creation,
/// and processing of AI Connections while ensuring sensitive settings never cross an environment boundary.
/// </summary>
[UdiDefinition(UmbracoAIConstants.UdiEntityType.Connection, UdiType.GuidUdi)]
public class UmbracoAIConnectionServiceConnector
    : UmbracoAIEntityServiceConnectorBase<AIConnectionArtifact, AIConnection>
{
    /// <summary>
    /// The value given to a required sensitive field that the target environment has no value for, so the
    /// connection can still be imported. The connection is imported inactive while this value is in place.
    /// </summary>
    internal const string MissingSecretPlaceholder = "UMBRACO_AI_DEPLOY_SECRET_NOT_SET";

    private readonly IAIConnectionService _connectionService;
    private readonly AIProviderCollection _providers;
    private readonly ILogger<UmbracoAIConnectionServiceConnector> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UmbracoAIConnectionServiceConnector"/> class.
    /// </summary>
    [Obsolete("Use the constructor that accepts an AIProviderCollection and ILogger. Will be removed in v20.")]
    public UmbracoAIConnectionServiceConnector(
        IAIConnectionService connectionService,
        UmbracoAIDeploySettingsAccessor settingsAccessor)
        : this(
            connectionService,
            settingsAccessor,
            StaticServiceProvider.Instance.GetRequiredService<AIProviderCollection>(),
            StaticServiceProvider.Instance.GetService<ILogger<UmbracoAIConnectionServiceConnector>>()
                ?? NullLogger<UmbracoAIConnectionServiceConnector>.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UmbracoAIConnectionServiceConnector"/> class.
    /// </summary>
    public UmbracoAIConnectionServiceConnector(
        IAIConnectionService connectionService,
        UmbracoAIDeploySettingsAccessor settingsAccessor,
        AIProviderCollection providers,
        ILogger<UmbracoAIConnectionServiceConnector> logger)
        : base(settingsAccessor)
    {
        _connectionService = connectionService;
        _providers = providers;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override int[] ProcessPasses => [1];

    /// <inheritdoc />
    protected override string[] ValidOpenSelectors => ["this", "this-and-descendants", "descendants"];

    /// <inheritdoc />
    protected override string OpenUdiName => "All Umbraco AI Connections";

    /// <inheritdoc />
    public override string UdiEntityType => UmbracoAIConstants.UdiEntityType.Connection;

    /// <inheritdoc />
    public override Task<AIConnection?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default)
        => _connectionService.GetConnectionAsync(id, cancellationToken);

    /// <inheritdoc />
    public override async IAsyncEnumerable<AIConnection> GetEntitiesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var connections = await _connectionService.GetConnectionsAsync(null, cancellationToken);
        foreach (var connection in connections)
        {
            yield return connection;
        }
    }

    /// <inheritdoc />
    public override string GetEntityName(AIConnection entity)
        => entity.Name;

    /// <inheritdoc />
    public override Task<AIConnectionArtifact?> GetArtifactAsync(
        GuidUdi udi,
        AIConnection? entity,
        CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            return Task.FromResult<AIConnectionArtifact?>(null);
        }

        var dependencies = new ArtifactDependencyCollection();

        Dictionary<string, JsonElement>? filteredSettings = FilterSettings(entity.Settings, GetSettingsSchema(entity.ProviderId));

        var artifact = new AIConnectionArtifact(udi, dependencies)
        {
            Alias = entity.Alias,
            Name = entity.Name,
            ProviderId = entity.ProviderId,
            Settings = filteredSettings is null ? null : JsonSerializer.SerializeToElement(filteredSettings),
            IsActive = entity.IsActive
        };

        return Task.FromResult<AIConnectionArtifact?>(artifact);
    }

    /// <inheritdoc />
    public override async Task ProcessAsync(
        ArtifactDeployState<AIConnectionArtifact, AIConnection> state,
        IDeployContext context,
        int pass,
        CancellationToken cancellationToken = default)
    {
        state.NextPass = GetNextPass(pass);

        switch (pass)
        {
            case 1:
                await Pass1Async(state, context, cancellationToken);
                break;
        }
    }

    private async Task Pass1Async(
        ArtifactDeployState<AIConnectionArtifact, AIConnection> state,
        IDeployContext context,
        CancellationToken cancellationToken)
    {
        var artifact = state.Artifact;
        AIEditableModelSchema? schema = GetSettingsSchema(artifact.ProviderId);

        // The same rule as export, so an artifact written before it existed can't carry a secret in.
        Dictionary<string, JsonElement>? settings = FilterSettings(artifact.Settings, schema);

        var placeholderFields = new List<string>();
        if (settings is not null)
        {
            placeholderFields = MergeTargetSecrets(settings, state.Entity?.Settings, schema);
        }

        // Create new connection or update existing one
        var connection = state.Entity
            ?? new AIConnection(artifact.Udi.Guid)
            {
                Alias = artifact.Alias!,
                Name = artifact.Name,
                ProviderId = artifact.ProviderId
            };

        // Update properties from artifact
        connection.Alias = artifact.Alias!;
        connection.Name = artifact.Name;
        connection.Settings = settings?.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value);

        // A connection holding a placeholder secret stays inactive until someone sets the real value here.
        connection.IsActive = artifact.IsActive && placeholderFields.Count == 0;

        if (placeholderFields.Count > 0)
        {
            _logger.LogWarning(
                "AI connection '{Alias}' was deployed without a value for {Fields}, which this environment doesn't have. "
                + "It has been imported inactive. Set the value(s) on this environment and activate the connection.",
                connection.Alias,
                string.Join(", ", placeholderFields));
        }

        state.Entity = await _connectionService.SaveConnectionAsync(connection, cancellationToken);
    }

    /// <summary>
    /// Fills in sensitive fields the artifact doesn't carry: from the target's own existing value when there
    /// is one, otherwise (for a required field) with <see cref="MissingSecretPlaceholder"/>.
    /// </summary>
    /// <returns>The keys of the fields left holding the placeholder.</returns>
    private static List<string> MergeTargetSecrets(
        Dictionary<string, JsonElement> settings,
        object? targetSettings,
        AIEditableModelSchema? schema)
    {
        UmbracoAISensitiveSettingsFilter.KeepTargetSensitiveValues(settings, targetSettings, schema);

        var placeholderFields = new List<string>();
        foreach (AIEditableModelField field in UmbracoAISensitiveSettingsFilter.GetSensitiveFields(schema))
        {
            bool hasValue = settings.TryGetValue(field.Key, out JsonElement value) && !UmbracoAISensitiveSettingsFilter.IsEmpty(value);
            if (!hasValue && UmbracoAISensitiveSettingsFilter.IsRequired(field))
            {
                value = JsonSerializer.SerializeToElement(MissingSecretPlaceholder);
                settings[field.Key] = value;
            }

            if (value.ValueKind == JsonValueKind.String && value.GetString() == MissingSecretPlaceholder)
            {
                placeholderFields.Add(field.Key);
            }
        }

        return placeholderFields;
    }

    private Dictionary<string, JsonElement>? FilterSettings(object? settings, AIEditableModelSchema? schema)
        => UmbracoAISensitiveSettingsFilter.Filter(
            settings,
            schema,
            _settingsAccessor.Settings.Connections.IgnoreSettings,
            _settingsAccessor.Settings.Connections.IncludeSensitiveSettings);

    private AIEditableModelSchema? GetSettingsSchema(string providerId)
        => _providers.GetById(providerId)?.GetSettingsSchema();
}
