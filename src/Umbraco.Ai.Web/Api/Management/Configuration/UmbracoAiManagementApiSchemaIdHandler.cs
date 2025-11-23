using Umbraco.Cms.Api.Common.OpenApi;

namespace Umbraco.Ai.Cms.Api.Management.Api.Management.Configuration;

/// <summary>
/// Schema ID handler for the Umbraco AI Management API.
/// </summary>
internal sealed class UmbracoAiManagementApiSchemaIdHandler : SchemaIdHandler
{
    /// <inheritdoc />
    public override bool CanHandle(Type type)
        => type.Namespace?.StartsWith(Constants.ManagementApi.ApiNamespacePrefix) is true;
}