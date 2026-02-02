namespace Umbraco.AI.Web.Api.Common.Configuration;

/// <summary>
/// Attribute to explicitly specify Swagger operation metadata including operation ID and tags.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is preferred over convention-based operation ID generation as it provides
/// explicit, clear naming that avoids errors from automatic singular/plural transformations.
/// </para>
/// <para>
/// When applied to a class (controller), the <see cref="Tags"/> property is inherited by all
/// action methods. This allows projects to define a base controller with a project-specific tag
/// that all endpoints inherit.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class SwaggerOperationAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the explicit operation ID for the endpoint.
    /// </summary>
    /// <remarks>
    /// Operation IDs are used in generated API clients to name methods.
    /// Use clear, consistent naming like "GetConnectionById", "GetAllConnections", "CreateConnection".
    /// </remarks>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the operation summary (short description shown in Swagger UI).
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets the operation description (longer description with more detail).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets additional tags to apply to the operation.
    /// </summary>
    /// <remarks>
    /// Tags are added alongside the default group tag (from ApiExplorerSettings.GroupName).
    /// Use this to add a project-level tag (e.g., "Umbraco AI") that can be used to filter
    /// operations when generating API clients.
    /// </remarks>
    public string[]? Tags { get; set; }
}
