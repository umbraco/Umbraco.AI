using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Settings;
using Umbraco.Extensions;

namespace Umbraco.Ai.Core.Providers;

/// <summary>
/// Attribute to mark AI provider implementations.
/// </summary>
/// <param name="id"></param>
/// <param name="name"></param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class AiProviderAttribute(string id, string name) : Attribute
{
    /// <summary>
    /// The unique identifier of the AI provider.
    /// </summary>
    public string Id { get; } = id;
    
    /// <summary>
    /// The display name of the AI provider.
    /// </summary>
    public string Name { get; } = name;
}

/// <summary>
/// Base class for AI providers.
/// </summary>
public abstract class AiProviderBase : IAiProvider
{
    /// <summary>
    /// The infrastructure services for AI providers.
    /// </summary>
    protected readonly IAiProviderInfrastructure Infrastructure;
    
    /// <summary>
    /// The capabilities supported by this provider.
    /// </summary>
    protected readonly List<IAiCapability> Capabilities = [];
    
    /// <inheritdoc />
    public string Id { get; }
    
    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public virtual Type? SettingsType => null;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AiProviderBase"/> class.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    protected AiProviderBase(IAiProviderInfrastructure infrastructure)
    {
        Infrastructure = infrastructure;
        
        var attribute = GetType().GetCustomAttribute<AiProviderAttribute>(inherit: false);
        if (attribute == null)
        {
            throw new InvalidOperationException($"The AI provider '{GetType().FullName}' is missing the required AiProviderAttribute.");
        }

        Id = attribute.Id;
        Name = attribute.Name;
    }
    
    /// <summary>
    /// Gets all capabilities supported by this provider.
    /// </summary>
    /// <returns></returns>
    public IReadOnlyCollection<IAiCapability> GetCapabilities()
        => Capabilities.AsReadOnly();

    /// <inheritdoc />
    public bool TryGetCapability<TCapability>(out TCapability? capability)
        where TCapability : class, IAiCapability
    {
        capability = Capabilities.OfType<TCapability>().FirstOrDefault();
        return capability != null;
    }

    /// <inheritdoc />
    public TCapability GetCapability<TCapability>()
        where TCapability : class, IAiCapability
        => TryGetCapability<TCapability>(out var capability) 
            ? capability! 
            : throw new InvalidOperationException($"The AI provider '{Id}' does not support the capability '{typeof(TCapability).FullName}'.");

    /// <inheritdoc />
    public bool HasCapability<TCapability>()
        where TCapability : class, IAiCapability
        => TryGetCapability<TCapability>(out _);

    /// <inheritdoc />
    public virtual IReadOnlyList<AiSettingDefinition> GetSettingDefinitions()
    {
        // Base implementation returns empty list (no settings)
        return Array.Empty<AiSettingDefinition>();
    }

    /// <summary>
    /// Adds a capability to this AI provider.
    /// </summary>
    /// <typeparam name="TCapability"></typeparam>
    protected void WithCapability<TCapability>()
        where TCapability : class, IAiCapability
    {
        Capabilities.Add(Infrastructure.CapabilityFactory.Create<TCapability>(this));
    }
}

/// <summary>
/// Base class for AI providers with typed settings support.
/// </summary>
/// <typeparam name="TSettings">The type of settings object required by this provider.</typeparam>
public abstract class AiProviderBase<TSettings> : AiProviderBase
    where TSettings : class, new()
{
    /// <inheritdoc />
    public override Type? SettingsType => typeof(TSettings);

    /// <summary>
    /// Initializes a new instance of the <see cref="AiProviderBase{TSettings}"/> class.
    /// </summary>
    /// <param name="infrastructure"></param>
    protected AiProviderBase(IAiProviderInfrastructure infrastructure)
        : base(infrastructure)
    { }

    /// <inheritdoc />
    public override IReadOnlyList<AiSettingDefinition> GetSettingDefinitions()
        => Infrastructure.SettingDefinitionBuilder.BuildForType<TSettings>(Id);

    /// <summary>
    /// Adds a capability to this AI provider.
    /// </summary>
    /// <typeparam name="TCapability"></typeparam>
    protected new void WithCapability<TCapability>()
        where TCapability : class, IAiCapability<TSettings>
    {
        Capabilities.Add(Infrastructure.CapabilityFactory.Create<TCapability>(this));
    }

    private static string InferEditorUiAlias(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        // TODO: DateTime, Enum, etc.
        
        if (underlyingType == typeof(string)) return "Umb.PropertyEditorUi.TextBox";
        if (underlyingType == typeof(int)) return "Umb.PropertyEditorUi.Integer";
        if (underlyingType == typeof(bool)) return "Umb.PropertyEditorUi.Toggle";
        if (underlyingType == typeof(decimal) || underlyingType == typeof(double) || underlyingType == typeof(float))
            return "Umb.PropertyEditorUi.Decimal";

        return "Umb.PropertyEditorUi.TextBox"; // fallback
    }
    
    private static IEnumerable<ValidationAttribute> InferValidationAttributes(PropertyInfo property)
    {
        var validationAttributes = property.GetCustomAttributes<ValidationAttribute>().ToList();
        
        // If the property is non-nullable and doesn't already have a Required attribute, add one
        if (!property.PropertyType.IsNullable() && !validationAttributes.OfType<RequiredAttribute>().Any())
        {
            validationAttributes.Add(new RequiredAttribute());
        }

        return validationAttributes;
    }
}