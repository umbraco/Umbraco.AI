using System.ComponentModel;
using Umbraco.AI.Core.Tools;

namespace Umbraco.AI.Tests.Common.Fakes;

/// <summary>
/// Fake implementation of <see cref="IAITool"/> for use in tests.
/// </summary>
public class FakeTool : IAITool
{
    public FakeTool(
        string id = "fake-tool",
        string name = "Fake Tool",
        string description = "A fake tool for testing",
        string? scopeId = null,
        bool isDestructive = false,
        IReadOnlyList<string>? tags = null)
    {
        Id = id;
        Name = name;
        Description = description;
        ScopeId = scopeId;
        IsDestructive = isDestructive;
        Tags = tags ?? [];
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string? ScopeId { get; }
    public bool IsDestructive { get; }
    public IReadOnlyList<string> Tags { get; }
    public Type? ArgsType => null;

    public Func<object?, CancellationToken, Task<object>>? ExecuteHandler { get; set; }

    public Task<object> ExecuteAsync(object? args, CancellationToken cancellationToken = default)
    {
        if (ExecuteHandler is not null)
        {
            return ExecuteHandler(args, cancellationToken);
        }

        return Task.FromResult<object>(new { Success = true });
    }

    public FakeTool WithExecuteHandler(Func<object?, CancellationToken, Task<object>> handler)
    {
        ExecuteHandler = handler;
        return this;
    }
}

/// <summary>
/// Fake implementation of a typed <see cref="IAITool"/> for use in tests.
/// </summary>
/// <typeparam name="TArgs">The arguments type.</typeparam>
public class FakeTypedTool<TArgs> : IAITool where TArgs : class
{
    public FakeTypedTool(
        string id = "fake-typed-tool",
        string name = "Fake Typed Tool",
        string description = "A fake typed tool for testing",
        string? scopeId = null,
        bool isDestructive = false,
        IReadOnlyList<string>? tags = null)
    {
        Id = id;
        Name = name;
        Description = description;
        ScopeId = scopeId;
        IsDestructive = isDestructive;
        Tags = tags ?? [];
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string? ScopeId { get; }
    public bool IsDestructive { get; }
    public IReadOnlyList<string> Tags { get; }
    public Type ArgsType => typeof(TArgs);

    public Func<TArgs, CancellationToken, Task<object>>? ExecuteHandler { get; set; }

    public Task<object> ExecuteAsync(object? args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (ExecuteHandler is not null)
        {
            return ExecuteHandler((TArgs)args, cancellationToken);
        }

        return Task.FromResult<object>(new { Success = true, Args = args });
    }

    public FakeTypedTool<TArgs> WithExecuteHandler(Func<TArgs, CancellationToken, Task<object>> handler)
    {
        ExecuteHandler = handler;
        return this;
    }
}

/// <summary>
/// Sample arguments for testing typed tools.
/// </summary>
public record FakeToolArgs(
    [property: Description("A test message")] string Message,
    [property: Description("A test count")] int Count = 1);

/// <summary>
/// Fake implementation of <see cref="IAISystemTool"/> for use in tests.
/// </summary>
public class FakeSystemTool : IAITool, IAISystemTool
{
    public FakeSystemTool(
        string id = "fake-system-tool",
        string name = "Fake System Tool",
        string description = "A fake system tool for testing",
        string? scopeId = null,
        bool isDestructive = false,
        IReadOnlyList<string>? tags = null)
    {
        Id = id;
        Name = name;
        Description = description;
        ScopeId = scopeId;
        IsDestructive = isDestructive;
        Tags = tags ?? [];
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string? ScopeId { get; }
    public bool IsDestructive { get; }
    public IReadOnlyList<string> Tags { get; }
    public Type? ArgsType => null;

    public Task<object> ExecuteAsync(object? args, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<object>(new { Success = true });
    }
}
