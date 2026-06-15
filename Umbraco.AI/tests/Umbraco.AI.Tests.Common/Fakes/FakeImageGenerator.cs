#pragma warning disable MEAI001 // IImageGenerator is experimental in M.E.AI

using Microsoft.Extensions.AI;

namespace Umbraco.AI.Tests.Common.Fakes;

/// <summary>
/// Fake implementation of <see cref="IImageGenerator"/> for use in tests.
/// </summary>
/// <remarks>
/// Records received requests/options and can expose arbitrary surrogate services via
/// <see cref="GetService"/> to simulate the provider-native escape hatch (e.g. an <c>ImageClient</c>
/// or <c>OpenAIClient</c> resolved through the scoped + middleware pipeline).
/// </remarks>
public sealed class FakeImageGenerator : IImageGenerator
{
    private readonly ImageGenerationResponse _response;
    private readonly Dictionary<Type, object> _services = new();

    public FakeImageGenerator(ImageGenerationResponse? response = null)
    {
        _response = response ?? new ImageGenerationResponse([new DataContent(new byte[] { 1, 2, 3 }, "image/png")])
        {
            Usage = new UsageDetails { InputTokenCount = 10, OutputTokenCount = 5, TotalTokenCount = 15 },
        };
    }

    /// <summary>The requests passed to <see cref="GenerateAsync"/>.</summary>
    public List<ImageGenerationRequest> ReceivedRequests { get; } = [];

    /// <summary>The options passed to <see cref="GenerateAsync"/>.</summary>
    public List<ImageGenerationOptions?> ReceivedOptions { get; } = [];

    /// <summary>
    /// Registers a surrogate service to be returned by <see cref="GetService"/> for the given type.
    /// </summary>
    public FakeImageGenerator RegisterService(Type type, object instance)
    {
        _services[type] = instance;
        return this;
    }

    public Task<ImageGenerationResponse> GenerateAsync(
        ImageGenerationRequest request,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ReceivedRequests.Add(request);
        ReceivedOptions.Add(options);
        return Task.FromResult(_response);
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceKey is null && _services.TryGetValue(serviceType, out var service))
        {
            return service;
        }

        if (serviceType == typeof(IImageGenerator) || serviceType == typeof(FakeImageGenerator))
        {
            return this;
        }

        return null;
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
