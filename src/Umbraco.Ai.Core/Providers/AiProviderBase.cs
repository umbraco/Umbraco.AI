using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Umbraco.Ai.Core.Models;
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
    /// The service provider for dependency injection.
    /// </summary>
    protected readonly IServiceProvider ServiceProvider;
    
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
    protected AiProviderBase(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        
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
    public bool TryGeCapability<TCapability>(out TCapability? capability)
        where TCapability : class, IAiCapability
    {
        capability = Capabilities.OfType<TCapability>().FirstOrDefault();
        return capability != null;
    }

    /// <inheritdoc />
    public TCapability GetCapability<TCapability>()
        where TCapability : class, IAiCapability
        => TryGeCapability<TCapability>(out var capability) 
            ? capability! 
            : throw new InvalidOperationException($"The AI provider '{Id}' does not support the capability '{typeof(TCapability).FullName}'.");

    /// <inheritdoc />
    public bool HasCapability<TCapability>()
        where TCapability : class, IAiCapability
        => TryGeCapability<TCapability>(out _);

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
        Capabilities.Add(ServiceProvider.CreateInstance<TCapability>(this));
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
    /// <param name="serviceProvider"></param>
    protected AiProviderBase(IServiceProvider serviceProvider)
        : base(serviceProvider)
    { }

    /// <inheritdoc />
    public override IReadOnlyList<AiSettingDefinition> GetSettingDefinitions()
    {
        var definitions = new List<AiSettingDefinition>();
        var properties = typeof(TSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            // Get custom attribute for metadata
            var attr = property.GetCustomAttribute<AiSettingAttribute>();
            
            definitions.Add(new AiSettingDefinition
            {
                Key = property.Name.ToLowerInvariant(),
                PropertyName = property.Name,
                PropertyType = property.PropertyType,
                Label = attr?.Label ?? $"#umbracoAiProviders_{Id.ToCamelCase()}Settings{property.Name}Label",
                Description = attr?.Description ?? $"#umbracoAiProviders_{Id.ToCamelCase()}Settings{property.Name}Description",
                EditorUiAlias = attr?.EditorUiAlias ?? InferEditorUiAlias(property.PropertyType),
                DefaultValue = attr?.DefaultValue,
                ValidationRules = InferValidationAttributes(property),
                SortOrder = attr?.SortOrder ?? 0
            });
        }

        return definitions;
    }

    /// <summary>
    /// Adds a capability to this AI provider.
    /// </summary>
    /// <typeparam name="TCapability"></typeparam>
    protected new void WithCapability<TCapability>()
        where TCapability : class, IAiCapability<TSettings>
    {
        Capabilities.Add(ServiceProvider.CreateInstance<TCapability>(this));
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