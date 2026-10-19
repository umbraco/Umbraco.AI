using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Tests.Unit.Api.Management;

/// <summary>
/// Shared xUnit collection for tests that swap <see cref="StaticServiceProvider.Instance"/> to
/// exercise an obsolete controller constructor's service-locator fallback.
/// </summary>
/// <remarks>
/// Every such test must join this one collection. <c>DisableParallelization</c> makes xUnit (2.x) run
/// this collection on its own, after the parallel collections, so no other test in the assembly
/// runs while <see cref="StaticServiceProvider.Instance"/> is swapped. Each test restores the
/// original instance in <c>Dispose</c>.
/// </remarks>
[CollectionDefinition(nameof(StaticServiceProviderTestCollection), DisableParallelization = true)]
public class StaticServiceProviderTestCollection;
