using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Tests.Common.Builders;

/// <summary>
/// Fluent builder for creating <see cref="AiConnection"/> instances in tests.
/// </summary>
public class AiConnectionBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _alias = $"test-connection-{Guid.NewGuid():N}";
    private string _name = "Test Connection";
    private string _providerId = "test-provider";
    private object? _settings;
    private bool _isActive = true;
    private DateTime _dateCreated = DateTime.UtcNow;
    private DateTime _dateModified = DateTime.UtcNow;

    public AiConnectionBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public AiConnectionBuilder WithAlias(string alias)
    {
        _alias = alias;
        return this;
    }

    public AiConnectionBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AiConnectionBuilder WithProviderId(string providerId)
    {
        _providerId = providerId;
        return this;
    }

    public AiConnectionBuilder WithSettings(object? settings)
    {
        _settings = settings;
        return this;
    }

    public AiConnectionBuilder WithSettings<TSettings>(Action<TSettings> configure) where TSettings : class, new()
    {
        var settings = new TSettings();
        configure(settings);
        _settings = settings;
        return this;
    }

    public AiConnectionBuilder IsActive(bool isActive = true)
    {
        _isActive = isActive;
        return this;
    }

    public AiConnectionBuilder WithDateCreated(DateTime dateCreated)
    {
        _dateCreated = dateCreated;
        return this;
    }

    public AiConnectionBuilder WithDateModified(DateTime dateModified)
    {
        _dateModified = dateModified;
        return this;
    }

    public AiConnection Build()
    {
        return new AiConnection
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

    public static implicit operator AiConnection(AiConnectionBuilder builder) => builder.Build();
}
