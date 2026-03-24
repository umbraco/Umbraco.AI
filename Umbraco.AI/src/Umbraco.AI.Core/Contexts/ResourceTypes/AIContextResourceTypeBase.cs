using System.Reflection;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Contexts.ResourceTypes;

/// <summary>
/// Base class for AI context resource types with strongly-typed settings and data.
/// </summary>
/// <typeparam name="TSettings">The settings model type for the resource.</typeparam>
public abstract class AIContextResourceTypeBase<TSettings> : AIContextResourceTypeBase<TSettings, TSettings>
    where TSettings : class, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIContextResourceTypeBase{TSettings}"/> class.
    /// </summary>
    /// <param name="infrastructure"></param>
    protected AIContextResourceTypeBase(IAIContextResourceTypeInfrastructure infrastructure)
        : base(infrastructure)
    { }

    /// <inheritdoc />
    public override Task<TSettings?> ResolveDataAsync(TSettings settings, CancellationToken cancellationToken = default)
        => Task.FromResult((TSettings?)settings);
}

/// <summary>
/// Base class for AI context resource types with strongly-typed data.
/// </summary>
/// <typeparam name="TSettings">The settings model type for the resource.</typeparam>
/// <typeparam name="TData">The data model type for the resource.</typeparam>
public abstract class AIContextResourceTypeBase<TSettings, TData> : IAIContextResourceType
    where TSettings : class, new()
    where TData : class, new()
{
    private readonly IAIContextResourceTypeInfrastructure _infrastructure;

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
    public Type SettingsType => typeof(TSettings);

    /// <inheritdoc />
    Type? IAIContextResourceType.SettingsType => SettingsType;

    /// <inheritdoc />
    public AIEditableModelSchema? GetSettingsSchema()
        => _infrastructure.SchemaBuilder.BuildForType<TSettings>(Id);

    /// <summary>
    /// Gets the data model type for this resource type.
    /// </summary>
    public Type DataType => typeof(TData);

    /// <inheritdoc />
    Type? IAIContextResourceType.DataType => DataType;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIContextResourceTypeBase{TSettings,TData}"/> class.
    /// </summary>
    /// <param name="infrastructure">The infrastructure dependencies.</param>
    /// <exception cref="InvalidOperationException">Thrown when the resource type is missing the required attribute.</exception>
    protected AIContextResourceTypeBase(IAIContextResourceTypeInfrastructure infrastructure)
    {
        _infrastructure = infrastructure;

        var attribute = GetType().GetCustomAttribute<AIContextResourceTypeAttribute>(inherit: false)
            ?? throw new InvalidOperationException(
                $"Resource type '{GetType().FullName}' is missing required [AIContextResourceType] attribute.");

        Id = attribute.Id;
        Name = attribute.Name;
    }

    /// <summary>
    /// Asynchronously resolves the resource data based on the provided settings.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual Task<TData?> ResolveDataAsync(TSettings settings, CancellationToken cancellationToken = default)
    {
        // Default implementation: return null. Override in derived classes to provide resolution logic.
        return Task.FromResult(default(TData));
    }

    /// <summary>
    /// Formats the strongly-typed resource data for injection into the LLM system prompt.
    /// </summary>
    /// <param name="data">The deserialized resource data.</param>
    /// <returns>Formatted text suitable for AI consumption.</returns>
    protected virtual string FormatDataForLlm(TData data)
    {
        // Default implementation: serialize to JSON
        return System.Text.Json.JsonSerializer.Serialize(data);
    }

    /// <inheritdoc />
    public async Task<object?> ResolveDataAsync(object? settings, CancellationToken cancellationToken = default)
    {
        if (settings is null)
            return null;

        // Use the model resolver to convert from stored format to typed model
        var typedSettings = _infrastructure.ModelResolver.ResolveModel<TSettings>(settings);
        if (typedSettings is null)
            return null;

        return await ResolveDataAsync(typedSettings, cancellationToken);
    }

    /// <inheritdoc />
    public string FormatDataForLlm(object? dataObject)
    {
        if (dataObject is null)
            return string.Empty;

        var typedData = dataObject as TData;
        if (typedData is null)
            return  string.Empty;

        return FormatDataForLlm(typedData);
    }
}
