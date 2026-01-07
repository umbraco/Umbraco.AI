using Umbraco.Cms.Core.Serialization;

namespace Umbraco.Ai.Core.Context.ResourceTypes;

/// <summary>
/// Default implementation of <see cref="IAiContextResourceTypeInfrastructure"/>.
/// </summary>
internal sealed class AiContextResourceTypeInfrastructure : IAiContextResourceTypeInfrastructure
{
    /// <inheritdoc />
    public IJsonSerializer JsonSerializer { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AiContextResourceTypeInfrastructure"/> class.
    /// </summary>
    /// <param name="jsonSerializer">The JSON serializer.</param>
    public AiContextResourceTypeInfrastructure(IJsonSerializer jsonSerializer)
    {
        JsonSerializer = jsonSerializer;
    }
}
