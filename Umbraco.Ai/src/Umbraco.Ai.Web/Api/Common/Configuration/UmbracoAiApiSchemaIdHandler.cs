using Umbraco.Cms.Api.Common.OpenApi;

namespace Umbraco.Ai.Web.Api.Management.Configuration;

/// <summary>
/// Schema ID handler for the Umbraco AI Management API.
/// </summary>
public class UmbracoAiApiSchemaIdHandler : SchemaIdHandler
{
    /// <summary>
    /// The namespace root for Umbraco AI API controllers.
    /// </summary>
    protected virtual string NameSpace => Constants.AppNamespaceRoot;
    
    /// <inheritdoc />
    public override bool CanHandle(Type type)
        => type.Namespace?.StartsWith(NameSpace) is true;
}