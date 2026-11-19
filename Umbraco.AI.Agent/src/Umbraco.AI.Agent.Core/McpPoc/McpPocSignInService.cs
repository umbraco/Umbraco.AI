using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Umbraco.AI.Core.Security;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Agent.Core.McpPoc;

/// <summary>
/// PROOF OF CONCEPT ONLY. Runs the OAuth sign-in for an MCP server ourselves, instead of the MCP
/// library's built-in flow, so a sign-in survives a restart or a callback on another web server.
/// </summary>
/// <remarks>
/// <para>
/// Everything the callback needs (PKCE verifier, client credentials, token endpoint) is written to
/// the database when the sign-in starts, keyed by the OAuth <c>state</c>. The callback reads it back
/// from there, never from memory. Values are encrypted with the same protector Umbraco.AI uses for
/// connection secrets.
/// </para>
/// <para>
/// Storage is Umbraco's key/value table so the POC needs no migrations. The real feature would use
/// its own tables, a database lock around token refresh, and a cleanup job for abandoned sign-ins.
/// </para>
/// </remarks>
internal sealed partial class McpPocSignInService : IMcpPocSignInService
{
    private const string KeyPrefix = "UmbracoAI.McpPoc.";
    private const string Consumed = "consumed";
    private static readonly TimeSpan PendingLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(60);

    private readonly IKeyValueService _keyValueService;
    private readonly IAISensitiveFieldProtector _protector;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<McpPocOptions> _options;
    private readonly ILogger<McpPocSignInService> _logger;

    // POC: process-local. Real feature: a database lock, so two web servers never refresh at once.
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _refreshLocks = new(StringComparer.OrdinalIgnoreCase);

    public McpPocSignInService(
        IKeyValueService keyValueService,
        IAISensitiveFieldProtector protector,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<McpPocOptions> options,
        ILogger<McpPocSignInService> logger)
    {
        _keyValueService = keyValueService;
        _protector = protector;
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public McpPocServer? GetServer(string alias)
        => _options.CurrentValue.Servers.FirstOrDefault(s => string.Equals(s.Alias, alias, StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc />
    public IReadOnlyList<McpPocServerStatus> GetServerStatuses()
        => _options.CurrentValue.Servers
            .Select(s =>
            {
                var token = Load<StoredToken>(TokenKey(s.Alias));
                var connected = token is not null && UrlMatches(token.ServerUrl, s.Url);
                return new McpPocServerStatus(s.Alias, s.Name, s.Url, connected, connected ? token!.ConnectedBy : null);
            })
            .ToList();

    /// <inheritdoc />
    public async Task<string> StartSignInAsync(string alias, string redirectUri, string connectedBy, CancellationToken cancellationToken)
    {
        var server = GetServer(alias) ?? throw new InvalidOperationException($"Unknown MCP server '{alias}'.");
        var http = _httpClientFactory.CreateClient(McpPocConstants.HttpClientName);

        // 1. Ask the MCP server where to sign in (RFC 9728 protected resource metadata).
        var resourceMetadata = await GetResourceMetadataAsync(http, server.Url, cancellationToken);
        var resource = resourceMetadata["resource"]?.GetValue<string>() ?? server.Url;
        var authorizationServer = resourceMetadata["authorization_servers"]?.AsArray().FirstOrDefault()?.GetValue<string>()
            ?? throw new InvalidOperationException("The MCP server does not name an authorization server.");
        var scopes = resourceMetadata["scopes_supported"]?.AsArray().Select(n => n!.GetValue<string>()).ToArray() ?? [];

        // 2. Read the authorization server's metadata (RFC 8414).
        var asMetadata = await GetAuthorizationServerMetadataAsync(http, authorizationServer, cancellationToken);
        var authorizationEndpoint = asMetadata["authorization_endpoint"]!.GetValue<string>();
        var tokenEndpoint = asMetadata["token_endpoint"]!.GetValue<string>();

        // 3. Register as a client once per server + redirect URI (RFC 7591), and keep the result.
        var client = Load<StoredClient>(ClientKey(alias));
        if (client is null || client.RedirectUri != redirectUri || client.AuthorizationServer != authorizationServer)
        {
            var registrationEndpoint = asMetadata["registration_endpoint"]?.GetValue<string>()
                ?? throw new InvalidOperationException(
                    "The authorization server doesn't support automatic client registration. "
                    + "The real feature would let an admin paste a client ID here.");
            client = await RegisterClientAsync(http, registrationEndpoint, redirectUri, authorizationServer, cancellationToken);
            Save(ClientKey(alias), client);
        }

        // 4. PKCE + state, saved to the database so any server (or a restarted one) can finish it.
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = Base64Url(RandomNumberGenerator.GetBytes(24));

        Save(PendingKey(state), new PendingSignIn
        {
            Alias = server.Alias,
            ServerUrl = server.Url,
            Verifier = verifier,
            RedirectUri = redirectUri,
            Resource = resource,
            TokenEndpoint = tokenEndpoint,
            ClientId = client.ClientId,
            ClientSecret = client.ClientSecret,
            ConnectedBy = connectedBy,
            ExpiresUtc = DateTimeOffset.UtcNow.Add(PendingLifetime),
        });

        var query = new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = client.ClientId,
            ["redirect_uri"] = redirectUri,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["state"] = state,
            ["resource"] = resource,
            ["scope"] = scopes.Length > 0 ? string.Join(' ', scopes) : null,
        };

        _logger.LogInformation("MCP POC: sign-in started for {Alias} by {User}", server.Alias, connectedBy);
        return authorizationEndpoint + "?" + string.Join('&', query
            .Where(kv => kv.Value is not null)
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"));
    }

    /// <inheritdoc />
    public async Task<string> CompleteSignInAsync(string state, string code, CancellationToken cancellationToken)
    {
        var key = PendingKey(state);
        var raw = _keyValueService.GetValue(key);
        if (string.IsNullOrEmpty(raw) || raw == Consumed)
        {
            throw new InvalidOperationException("This sign-in link is unknown or was already used.");
        }

        // One-time use, safe across web servers: only the caller that swaps the value wins.
        if (!_keyValueService.TrySetValue(key, raw, Consumed))
        {
            throw new InvalidOperationException("This sign-in was already completed.");
        }

        var pending = Deserialize<PendingSignIn>(raw)
            ?? throw new InvalidOperationException("The saved sign-in could not be read.");
        if (pending.ExpiresUtc < DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("The sign-in took too long. Start again.");
        }

        var http = _httpClientFactory.CreateClient(McpPocConstants.HttpClientName);
        var token = await RequestTokenAsync(
            http,
            pending.TokenEndpoint,
            new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = pending.RedirectUri,
                ["code_verifier"] = pending.Verifier,
                ["client_id"] = pending.ClientId,
                ["client_secret"] = pending.ClientSecret ?? string.Empty,
                ["resource"] = pending.Resource,
            },
            cancellationToken);

        Save(TokenKey(pending.Alias), new StoredToken
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            ExpiresUtc = token.ExpiresUtc,
            TokenEndpoint = pending.TokenEndpoint,
            ClientId = pending.ClientId,
            ClientSecret = pending.ClientSecret,
            Resource = pending.Resource,
            ServerUrl = pending.ServerUrl,
            ConnectedBy = pending.ConnectedBy,
        });

        _logger.LogInformation("MCP POC: {Alias} connected by {User}", pending.Alias, pending.ConnectedBy);
        return pending.Alias;
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(string alias, CancellationToken cancellationToken)
    {
        var server = GetServer(alias);
        var token = Load<StoredToken>(TokenKey(alias));

        // Tokens are tied to the server URL: an edited URL never receives the old token.
        if (server is null || token is null || !UrlMatches(token.ServerUrl, server.Url))
        {
            return null;
        }

        if (token.ExpiresUtc is null || token.ExpiresUtc > DateTimeOffset.UtcNow.Add(RefreshSkew))
        {
            return token.AccessToken;
        }

        if (string.IsNullOrEmpty(token.RefreshToken))
        {
            return null;
        }

        var gate = _refreshLocks.GetOrAdd(alias, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            // Someone else may have refreshed while we waited.
            token = Load<StoredToken>(TokenKey(alias));
            if (token is null)
            {
                return null;
            }

            if (token.ExpiresUtc > DateTimeOffset.UtcNow.Add(RefreshSkew))
            {
                return token.AccessToken;
            }

            var http = _httpClientFactory.CreateClient(McpPocConstants.HttpClientName);
            var refreshed = await RequestTokenAsync(
                http,
                token.TokenEndpoint,
                new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = token.RefreshToken!,
                    ["client_id"] = token.ClientId,
                    ["client_secret"] = token.ClientSecret ?? string.Empty,
                    ["resource"] = token.Resource,
                },
                cancellationToken);

            token.AccessToken = refreshed.AccessToken;
            token.RefreshToken = refreshed.RefreshToken ?? token.RefreshToken;
            token.ExpiresUtc = refreshed.ExpiresUtc;
            Save(TokenKey(alias), token);

            _logger.LogInformation("MCP POC: refreshed token for {Alias}", alias);
            return token.AccessToken;
        }
        finally
        {
            gate.Release();
        }
    }

    private static async Task<JsonObject> GetResourceMetadataAsync(HttpClient http, string serverUrl, CancellationToken ct)
    {
        // Prefer the address the server gives in its 401 challenge.
        string? metadataUrl = null;
        using (var probe = new HttpRequestMessage(HttpMethod.Post, serverUrl)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        })
        {
            probe.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            probe.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            using var response = await http.SendAsync(probe, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                foreach (var header in response.Headers.WwwAuthenticate)
                {
                    var match = ResourceMetadataRegex().Match(header.Parameter ?? string.Empty);
                    if (match.Success)
                    {
                        metadataUrl = match.Groups[1].Value;
                    }
                }
            }
        }

        if (metadataUrl is null)
        {
            var uri = new Uri(serverUrl);
            metadataUrl = $"{uri.GetLeftPart(UriPartial.Authority)}/.well-known/oauth-protected-resource{uri.AbsolutePath.TrimEnd('/')}";
        }

        return await http.GetFromJsonAsync<JsonObject>(metadataUrl, ct)
            ?? throw new InvalidOperationException("The MCP server returned no sign-in metadata.");
    }

    private static async Task<JsonObject> GetAuthorizationServerMetadataAsync(HttpClient http, string issuer, CancellationToken ct)
    {
        var uri = new Uri(issuer);
        var authority = uri.GetLeftPart(UriPartial.Authority);
        var path = uri.AbsolutePath.TrimEnd('/');
        foreach (var candidate in new[]
                 {
                     $"{authority}/.well-known/oauth-authorization-server{path}",
                     $"{authority}/.well-known/openid-configuration{path}",
                     $"{issuer.TrimEnd('/')}/.well-known/openid-configuration",
                 })
        {
            using var response = await http.GetAsync(candidate, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<JsonObject>(ct)
                    ?? throw new InvalidOperationException("Empty authorization server metadata.");
            }
        }

        throw new InvalidOperationException($"Could not find authorization server metadata for '{issuer}'.");
    }

    private static async Task<StoredClient> RegisterClientAsync(
        HttpClient http, string registrationEndpoint, string redirectUri, string authorizationServer, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync(
            registrationEndpoint,
            new JsonObject
            {
                ["client_name"] = "Umbraco AI (MCP POC)",
                ["redirect_uris"] = new JsonArray(redirectUri),
                ["grant_types"] = new JsonArray("authorization_code", "refresh_token"),
                ["response_types"] = new JsonArray("code"),
                ["token_endpoint_auth_method"] = "client_secret_post",
            },
            ct);

        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Client registration failed ({(int)response.StatusCode}): {body}");
        }

        var json = JsonNode.Parse(body)!.AsObject();
        return new StoredClient
        {
            ClientId = json["client_id"]!.GetValue<string>(),
            ClientSecret = json["client_secret"]?.GetValue<string>(),
            RedirectUri = redirectUri,
            AuthorizationServer = authorizationServer,
        };
    }

    private static async Task<TokenResult> RequestTokenAsync(
        HttpClient http, string tokenEndpoint, Dictionary<string, string> form, CancellationToken ct)
    {
        using var response = await http.PostAsync(tokenEndpoint, new FormUrlEncodedContent(form), ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Token request failed ({(int)response.StatusCode}): {body}");
        }

        var json = JsonNode.Parse(body)!.AsObject();
        var expiresIn = json["expires_in"]?.GetValue<int>();
        return new TokenResult(
            json["access_token"]!.GetValue<string>(),
            json["refresh_token"]?.GetValue<string>(),
            expiresIn is null ? null : DateTimeOffset.UtcNow.AddSeconds(expiresIn.Value));
    }

    private T? Load<T>(string key)
        where T : class
    {
        var raw = _keyValueService.GetValue(key);
        return string.IsNullOrEmpty(raw) || raw == Consumed ? null : Deserialize<T>(raw);
    }

    private T? Deserialize<T>(string raw)
        where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(_protector.Unprotect(raw)!);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MCP POC: could not read a stored value");
            return null;
        }
    }

    private void Save<T>(string key, T value)
        => _keyValueService.SetValue(key, _protector.Protect(JsonSerializer.Serialize(value))!);

    private static bool UrlMatches(string a, string b)
        => string.Equals(a.TrimEnd('/'), b.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string PendingKey(string state) => $"{KeyPrefix}Pending.{state}";

    private static string TokenKey(string alias) => $"{KeyPrefix}Token.{alias.ToLowerInvariant()}";

    private static string ClientKey(string alias) => $"{KeyPrefix}Client.{alias.ToLowerInvariant()}";

    [GeneratedRegex("resource_metadata=\"([^\"]+)\"")]
    private static partial Regex ResourceMetadataRegex();

    private sealed record TokenResult(string AccessToken, string? RefreshToken, DateTimeOffset? ExpiresUtc);

    private sealed class PendingSignIn
    {
        public string Alias { get; set; } = string.Empty;
        public string ServerUrl { get; set; } = string.Empty;
        public string Verifier { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string TokenEndpoint { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string? ClientSecret { get; set; }
        public string ConnectedBy { get; set; } = string.Empty;
        public DateTimeOffset ExpiresUtc { get; set; }
    }

    private sealed class StoredClient
    {
        public string ClientId { get; set; } = string.Empty;
        public string? ClientSecret { get; set; }
        public string RedirectUri { get; set; } = string.Empty;
        public string AuthorizationServer { get; set; } = string.Empty;
    }

    private sealed class StoredToken
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public DateTimeOffset? ExpiresUtc { get; set; }
        public string TokenEndpoint { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string? ClientSecret { get; set; }
        public string Resource { get; set; } = string.Empty;
        public string ServerUrl { get; set; } = string.Empty;
        public string ConnectedBy { get; set; } = string.Empty;
    }
}
