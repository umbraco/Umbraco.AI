using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Providers;

internal sealed class AIProviderInfrastructure(
    IAiCapabilityFactory capabilityFactory,
    IAiEditableModelSchemaBuilder schemaBuilder)
    : IAiProviderInfrastructure
{
    public IAiCapabilityFactory CapabilityFactory { get; } = capabilityFactory;

    public IAiEditableModelSchemaBuilder SchemaBuilder { get; } = schemaBuilder;
}