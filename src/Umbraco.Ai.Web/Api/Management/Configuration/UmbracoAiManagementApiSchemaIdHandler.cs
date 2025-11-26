using Umbraco.Ai.Web.Api;
using Umbraco.Cms.Api.Common.OpenApi;

namespace Umbraco.Ai.Web.Api.Management.Configuration;

/// <summary>
/// Schema ID handler for the Umbraco AI Management API.
/// </summary>
internal sealed class UmbracoAiManagementApiSchemaIdHandler : SchemaIdHandler
{
    /// <inheritdoc />
    public override bool CanHandle(Type type)
        => type.Namespace?.StartsWith(Constants.ManagementApi.ApiNamespacePrefix) is true;
}