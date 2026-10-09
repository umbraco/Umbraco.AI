namespace Umbraco.AI.Core.ModelFacts;

/// <summary>
/// Configuration options for model facts, bound to the <c>Umbraco:AI:ModelFacts</c> configuration section.
/// </summary>
public class AIModelFactOptions
{
    /// <summary>
    /// Gets or sets the maximum time each <see cref="IAIModelFactProvider"/> may take per request.
    /// Providers that exceed it are skipped. Default is 2 seconds.
    /// </summary>
    public TimeSpan ProviderTimeout { get; set; } = TimeSpan.FromSeconds(2);
}
