using System.Reflection;

namespace Umbraco.Ai.Core.Context.ResourceTypes;

/// <summary>
/// Base class for AI context resource types with strongly-typed data.
/// </summary>
/// <typeparam name="TData">The data model type for the resource.</typeparam>
public abstract class AiContextResourceTypeBase<TData> : IAiContextResourceType
    where TData : class
{
    private readonly IAiContextResourceTypeInfrastructure _infrastructure;

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public abstract string? Description { get; }

    /// <inheritdoc />
    public abstract string? Icon { get; }

    /// <summary>
    /// Gets the data model type for this resource type.
    /// </summary>
    public Type DataType => typeof(TData);

    /// <summary>
    /// Initializes a new instance of the <see cref="AiContextResourceTypeBase{TData}"/> class.
    /// </summary>
    /// <param name="infrastructure">The infrastructure dependencies.</param>
    /// <exception cref="InvalidOperationException">Thrown when the resource type is missing the required attribute.</exception>
    protected AiContextResourceTypeBase(IAiContextResourceTypeInfrastructure infrastructure)
    {
        _infrastructure = infrastructure;

        var attribute = GetType().GetCustomAttribute<AiContextResourceTypeAttribute>(inherit: false)
            ?? throw new InvalidOperationException(
                $"Resource type '{GetType().FullName}' is missing required [AiContextResourceType] attribute.");

        Id = attribute.Id;
        Name = attribute.Name;
    }

    /// <summary>
    /// Formats the strongly-typed resource data for injection into the system prompt.
    /// </summary>
    /// <param name="data">The deserialized resource data.</param>
    /// <returns>Formatted text suitable for AI consumption.</returns>
    protected abstract string FormatForInjection(TData data);

    /// <inheritdoc />
    public string FormatForInjection(string jsonData)
    {
        if (string.IsNullOrWhiteSpace(jsonData))
            return string.Empty;

        var data = _infrastructure.JsonSerializer.Deserialize<TData>(jsonData);
        if (data is null)
            return string.Empty;

        return FormatForInjection(data);
    }
}
