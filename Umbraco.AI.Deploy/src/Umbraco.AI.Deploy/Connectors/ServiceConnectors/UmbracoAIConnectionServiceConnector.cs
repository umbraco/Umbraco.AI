using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Deploy.Artifacts;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;

namespace Umbraco.AI.Deploy.Connectors.ServiceConnectors;

/// <summary>
/// Service connector for Umbraco AI Connections, responsible for synchronizing AIConnection entities with their
/// corresponding AIConnectionArtifact during deploy operations. This connector handles retrieval, artifact creation,
/// and processing of AI Connections while ensuring sensitive settings are filtered according to configuration.
/// </summary>
/// <param name="connectionService"></param>
/// <param name="settingsAccessor"></param>
[UdiDefinition(UmbracoAIConstants.UdiEntityType.Connection, UdiType.GuidUdi)]
public class UmbracoAIConnectionServiceConnector(
    IAIConnectionService connectionService,
    UmbracoAIDeploySettingsAccessor settingsAccessor)
    : UmbracoAIEntityServiceConnectorBase<AIConnectionArtifact, AIConnection>(settingsAccessor)
{
    /// <inheritdoc />
    protected override int[] ProcessPasses => [2];

    /// <inheritdoc />
    protected override string[] ValidOpenSelectors => ["this", "this-and-descendants", "descendants"];

    /// <inheritdoc />
    protected override string OpenUdiName => "All Umbraco AI Connections";

    /// <inheritdoc />
    public override string UdiEntityType => UmbracoAIConstants.UdiEntityType.Connection;

    /// <inheritdoc />
    public override Task<AIConnection?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default)
        => connectionService.GetConnectionAsync(id, cancellationToken);

    /// <inheritdoc />
    public override async IAsyncEnumerable<AIConnection> GetEntitiesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var connections = await connectionService.GetConnectionsAsync(null, cancellationToken);
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

        // Filter sensitive settings before serialization
        var filteredSettings = FilterSensitiveSettings(entity.Settings);

        var artifact = new AIConnectionArtifact(udi, dependencies)
        {
            Alias = entity.Alias,
            Name = entity.Name,
            ProviderId = entity.ProviderId,
            Settings = filteredSettings,
            IsActive = entity.IsActive,
            DateCreated = entity.DateCreated,
            DateModified = entity.DateModified,
            CreatedByUserId = entity.CreatedByUserId,
            ModifiedByUserId = entity.ModifiedByUserId,
            Version = entity.Version
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
            case 2:
                await Pass2Async(state, context, cancellationToken);
                break;
        }
    }

    private async Task Pass2Async(
        ArtifactDeployState<AIConnectionArtifact, AIConnection> state,
        IDeployContext context,
        CancellationToken cancellationToken)
    {
        var artifact = state.Artifact;

        // Deserialize settings from JsonElement to Dictionary
        Dictionary<string, object?>? settings = null;
        if (artifact.Settings.HasValue)
        {
            settings = JsonSerializer.Deserialize<Dictionary<string, object?>>(artifact.Settings.Value);
        }

        if (state.Entity == null)
        {
            // Create new connection
            var connection = new AIConnection
            {
                Alias = artifact.Alias!,
                Name = artifact.Name,
                ProviderId = artifact.ProviderId,
                Settings = settings,
                IsActive = artifact.IsActive,
                CreatedByUserId = artifact.CreatedByUserId,
                ModifiedByUserId = artifact.ModifiedByUserId
            };

            state.Entity = await connectionService.SaveConnectionAsync(connection, cancellationToken);
        }
        else
        {
            // Update existing connection
            var connection = state.Entity;
            connection.Name = artifact.Name;
            connection.Settings = settings;
            connection.IsActive = artifact.IsActive;
            connection.ModifiedByUserId = artifact.ModifiedByUserId;

            state.Entity = await connectionService.SaveConnectionAsync(connection, cancellationToken);
        }
    }

    /// <summary>
    /// Filters sensitive settings before deployment using three-layer filtering:
    /// 1. IgnoreSettings (highest precedence) - specific field names
    /// 2. IgnoreSensitive - fields marked [AIField(IsSensitive=true)]
    /// 3. IgnoreEncrypted - encrypted values (ENC:...) but allows $ references
    /// </summary>
    private JsonElement? FilterSensitiveSettings(object? settings)
    {
        if (settings == null)
        {
            return null;
        }

        // Convert settings to dictionary for filtering
        Dictionary<string, object?>? settingsDict;
        if (settings is Dictionary<string, object?> dict)
        {
            settingsDict = dict;
        }
        else
        {
            var json = JsonSerializer.SerializeToElement(settings);
            settingsDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
        }

        if (settingsDict == null)
        {
            return null;
        }

        // Get settings type for attribute checking
        var settingsType = settings.GetType();

        var filtered = settingsDict
            .Where(kvp =>
            {
                var key = kvp.Key;
                var value = kvp.Value?.ToString();

                // Layer 1: IgnoreSettings - highest precedence, always block these specific fields
                if (_settingsAccessor.Settings.Connections.IgnoreSettings.Contains(key))
                {
                    return false;
                }

                // Check if field is marked [AIField(IsSensitive = true)]
                var property = settingsType.GetProperty(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                var fieldAttr = property?.GetCustomAttribute<AIFieldAttribute>();
                bool isFieldMarkedSensitive = fieldAttr?.IsSensitive == true;

                // Layer 2: IgnoreSensitive - block sensitive fields entirely (including $ refs)
                if (_settingsAccessor.Settings.Connections.IgnoreSensitive && isFieldMarkedSensitive)
                {
                    return false;
                }

                // Check value characteristics
                bool isConfigReference = value?.StartsWith("$") == true;
                bool isEncryptedValue = value?.StartsWith("ENC:") == true;

                // Layer 3: IgnoreEncrypted - block encrypted values but allow $ config references
                if (_settingsAccessor.Settings.Connections.IgnoreEncrypted && isEncryptedValue)
                {
                    return !isConfigReference;  // Block encrypted, allow $ refs
                }

                return true;  // Include all other fields
            })
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return JsonSerializer.SerializeToElement(filtered);
    }
}
