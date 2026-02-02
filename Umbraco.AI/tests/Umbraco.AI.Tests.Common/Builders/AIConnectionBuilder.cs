using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Tests.Common.Builders;

/// <summary>
/// Fluent builder for creating <see cref="AIConnection"/> instances in tests.
/// </summary>
public class AIConnectionBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _alias = $"test-connection-{Guid.NewGuid():N}";
    private string _name = "Test Connection";
    private string _providerId = "test-provider";
    private object? _settings;
    private bool _isActive = true;
    private DateTime _dateCreated = DateTime.UtcNow;
    private DateTime _dateModified = DateTime.UtcNow;

    public AIConnectionBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public AIConnectionBuilder WithAlias(string alias)
    {
        _alias = alias;
        return this;
    }

    public AIConnectionBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AIConnectionBuilder WithProviderId(string providerId)
    {
        _providerId = providerId;
        return this;
    }

    public AIConnectionBuilder WithSettings(object? settings)
    {
        _settings = settings;
        return this;
    }

    public AIConnectionBuilder WithSettings<TSettings>(Action<TSettings> configure) where TSettings : class, new()
    {
        var settings = new TSettings();
        configure(settings);
        _settings = settings;
        return this;
    }

    public AIConnectionBuilder IsActive(bool isActive = true)
    {
        _isActive = isActive;
        return this;
    }

    public AIConnectionBuilder WithDateCreated(DateTime dateCreated)
    {
        _dateCreated = dateCreated;
        return this;
    }

    public AIConnectionBuilder WithDateModified(DateTime dateModified)
    {
        _dateModified = dateModified;
        return this;
    }

    public AIConnection Build()
    {
        return new AIConnection
        {
            Id = _id,
            Alias = _alias,
            Name = _name,
            ProviderId = _providerId,
            Settings = _settings,
            IsActive = _isActive,
            DateCreated = _dateCreated,
            DateModified = _dateModified
        };
    }

    public static implicit operator AIConnection(AIConnectionBuilder builder) => builder.Build();
}
