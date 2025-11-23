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
    
    /// <summary>
    /// Adds a capability to this AI provider.
    /// </summary>
    /// <typeparam name="TCapability"></typeparam>
    protected void WithCapability<TCapability>()
        where TCapability : class, IAiCapability
    {
        Capabilities.Add(ServiceProvider.CreateInstance<TCapability>());
    }
}

/// <summary>
/// Base class for AI providers with typed settings support.
/// </summary>
/// <typeparam name="TSettings">The type of settings object required by this provider.</typeparam>
public abstract class AiProviderBase<TSettings> : AiProviderBase
    where TSettings : class, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiProviderBase{TSettings}"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    protected AiProviderBase(IServiceProvider serviceProvider)
        : base(serviceProvider)
    { }

    /// <summary>
    /// Adds a capability to this AI provider.
    /// </summary>
    /// <typeparam name="TCapability"></typeparam>
    protected new void WithCapability<TCapability>()
        where TCapability : class, IAiCapability<TSettings>
    {
        Capabilities.Add(ServiceProvider.CreateInstance<TCapability>());
    }
}