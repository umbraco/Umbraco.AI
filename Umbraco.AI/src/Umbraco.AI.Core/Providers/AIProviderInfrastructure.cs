using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Providers;

internal sealed class AIProviderInfrastructure(
    IAICapabilityFactory capabilityFactory,
    IAIEditableModelSchemaBuilder schemaBuilder)
    : IAIProviderInfrastructure
{
    public IAICapabilityFactory CapabilityFactory { get; } = capabilityFactory;

    public IAIEditableModelSchemaBuilder SchemaBuilder { get; } = schemaBuilder;
}