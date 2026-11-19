using System.Collections.Concurrent;
using System.Net.Http.Headers;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Client;

namespace Umbraco.AI.Agent.Core.McpPoc;

/// <summary>
/// PROOF OF CONCEPT ONLY. Supplies tools from signed-in MCP servers to an agent run.
/// </summary>
internal interface IMcpPocToolSource
{
    /// <summary>
    /// Gets the allowed MCP tools for an agent. A server that is down or not signed in is skipped,
    /// never failing the whole turn.
    /// </summary>
    Task<IReadOnlyList<AITool>> GetToolsAsync(string agentAlias, CancellationToken cancellationToken);
}

/// <inheritdoc cref="IMcpPocToolSource" />
internal sealed class McpPocToolSource : IMcpPocToolSource, IAsyncDisposable
{
    private readonly IMcpPocSignInService _signInService;
    private readonly IOptionsMonitor<McpPocOptions> _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<McpPocToolSource> _logger;
    private readonly ConcurrentDictionary<string, Task<McpClient>> _clients = new(StringComparer.OrdinalIgnoreCase);

    public McpPocToolSource(
        IMcpPocSignInService signInService,
        IOptionsMonitor<McpPocOptions> options,
        ILoggerFactory loggerFactory)
    {
        _signInService = signInService;
        _options = options;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<McpPocToolSource>();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AITool>> GetToolsAsync(string agentAlias, CancellationToken cancellationToken)
    {
        var result = new List<AITool>();

        foreach (var server in _options.CurrentValue.Servers)
        {
            if (server.AgentAliases.Count > 0
                && !server.AgentAliases.Contains(agentAlias, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (await _signInService.GetAccessTokenAsync(server.Alias, cancellationToken) is null)
            {
                _logger.LogInformation("MCP POC: {Alias} is not signed in; its tools are left out", server.Alias);
                continue;
            }

            try
            {
                var client = await GetClientAsync(server, cancellationToken);
                var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);

                foreach (var tool in tools)
                {
                    // Allowlist by name: anything the admin didn't approve never reaches the model.
                    if (!server.AllowedTools.Contains(tool.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Prefix with the server alias so two servers can both have "search".
                    result.Add(tool.WithName($"{server.Alias}_{tool.Name}"));
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "MCP POC: {Alias} is unavailable; its tools are left out", server.Alias);
                _clients.TryRemove(ClientKey(server), out _);
            }
        }

        return result;
    }

    private Task<McpClient> GetClientAsync(McpPocServer server, CancellationToken cancellationToken)
        => _clients.GetOrAdd(ClientKey(server), _ => CreateClientAsync(server, cancellationToken));

    private async Task<McpClient> CreateClientAsync(McpPocServer server, CancellationToken cancellationToken)
    {
        // Our handler adds the stored token to every request, so the library never runs its own sign-in.
        var httpClient = new HttpClient(new BearerTokenHandler(server.Alias, _signInService)
        {
            InnerHandler = new SocketsHttpHandler(),
        });

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(server.Url),
                Name = server.Alias,
                TransportMode = HttpTransportMode.StreamableHttp,
            },
            httpClient,
            _loggerFactory,
            ownsHttpClient: true);

        return await McpClient.CreateAsync(transport, loggerFactory: _loggerFactory, cancellationToken: cancellationToken);
    }

    private static string ClientKey(McpPocServer server) => $"{server.Alias}|{server.Url}";

    public async ValueTask DisposeAsync()
    {
        foreach (var task in _clients.Values)
        {
            if (task.IsCompletedSuccessfully)
            {
                await task.Result.DisposeAsync();
            }
        }
    }

    private sealed class BearerTokenHandler(string alias, IMcpPocSignInService signInService) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await signInService.GetAccessTokenAsync(alias, cancellationToken);
            if (token is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
