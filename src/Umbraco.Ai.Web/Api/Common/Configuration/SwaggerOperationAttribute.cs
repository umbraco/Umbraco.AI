namespace Umbraco.Ai.Web.Api.Common.Configuration;

/// <summary>
/// Attribute to explicitly specify Swagger operation metadata including operation ID.
/// </summary>
/// <remarks>
/// This attribute is preferred over convention-based operation ID generation as it provides
/// explicit, clear naming that avoids errors from automatic singular/plural transformations.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
internal sealed class SwaggerOperationAttribute : Attribute
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
}
