using System.Reflection;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Contexts.ResourceTypes;

/// <summary>
/// Base class for AI context resource types with strongly-typed data.
/// </summary>
/// <typeparam name="TData">The data model type for the resource.</typeparam>
public abstract class AIContextResourceTypeBase<TData> : IAiContextResourceType
    where TData : class, new()
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

    /// <inheritdoc />
    Type? IAiContextResourceType.DataType => DataType;

    /// <inheritdoc />
    public AIEditableModelSchema? GetDataSchema()
        => _infrastructure.SchemaBuilder.BuildForType<TData>(Id);

    /// <summary>
    /// Initializes a new instance of the <see cref="AIContextResourceTypeBase{TData}"/> class.
    /// </summary>
    /// <param name="infrastructure">The infrastructure dependencies.</param>
    /// <exception cref="InvalidOperationException">Thrown when the resource type is missing the required attribute.</exception>
    protected AIContextResourceTypeBase(IAiContextResourceTypeInfrastructure infrastructure)
    {
        _infrastructure = infrastructure;

        var attribute = GetType().GetCustomAttribute<AIContextResourceTypeAttribute>(inherit: false)
            ?? throw new InvalidOperationException(
                $"Resource type '{GetType().FullName}' is missing required [AIContextResourceType] attribute.");

        Id = attribute.Id;
        Name = attribute.Name;
    }

    /// <summary>
    /// Formats the strongly-typed resource data for injection into the LLM system prompt.
    /// </summary>
    /// <param name="data">The deserialized resource data.</param>
    /// <returns>Formatted text suitable for AI consumption.</returns>
    protected abstract string FormatForLlm(TData data);

    /// <inheritdoc />
    public string FormatForLlm(object? dataObject)
    {
        if (dataObject is null)
            return string.Empty;

        // Use the model resolver to convert from stored format to typed model
        var data = _infrastructure.ModelResolver.ResolveModel<TData>(Id, dataObject);
        if (data is null)
            return string.Empty;

        return FormatForLlm(data);
    }
}
