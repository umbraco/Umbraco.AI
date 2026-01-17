using Umbraco.Ai.Core.EditableModels;

namespace Umbraco.Ai.Core.Providers;

internal sealed class AiProviderInfrastructure(
    IAiCapabilityFactory capabilityFactory,
    IAiEditableModelSchemaBuilder schemaBuilder)
    : IAiProviderInfrastructure
{
    public IAiCapabilityFactory CapabilityFactory { get; } = capabilityFactory;

    public IAiEditableModelSchemaBuilder SchemaBuilder { get; } = schemaBuilder;
}