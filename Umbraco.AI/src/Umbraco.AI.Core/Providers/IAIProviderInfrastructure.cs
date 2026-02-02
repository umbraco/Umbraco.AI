using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Defines the infrastructure components required by AI providers.
/// </summary>
public interface IAIProviderInfrastructure
{
    /// <summary>
    /// Factory for creating AI capability instances.
    /// </summary>
    IAICapabilityFactory CapabilityFactory { get; }

    /// <summary>
    /// Builder for editable model schemas.
    /// </summary>
    IAIEditableModelSchemaBuilder SchemaBuilder { get; }
}