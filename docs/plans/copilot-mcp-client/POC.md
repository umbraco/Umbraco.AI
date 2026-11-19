# Proof of concept: Copilot as MCP client

> **Status:** Throwaway proof of concept, built 19-11-2026 on `v18/topic/mcp-client-poc`. Never
> merge this branch. It tests the three unknowns from the feasibility study against the MCP C#
> SDK's own sample server.

**It works.** Copilot agents called a tool on an OAuth-protected MCP server and answered from its
data, on OpenAI and Anthropic. The sign-in survived a site restart halfway through, and the token
refreshed by itself.

## What was tested

| Check | Result |
| --- | --- |
| Find where to sign in from the MCP server alone (401 → resource metadata → auth server metadata) | Pass |
| Register as a client automatically (dynamic client registration) | Pass |
| Start sign-in, restart the site, then finish sign-in | Pass |
| Reuse a finished sign-in link | Refused, as intended |
| OpenAI `gpt-4o` calls the tool and answers from it | Pass |
| Anthropic `claude-sonnet-5-5` calls the tool and answers from it | Pass |
| Server not signed in: tools left out, chat still answers | Pass |
| Token near expiry: refreshed automatically, new token saved | Pass |
| Only allowlisted tools reach the model, prefixed with the server alias | Pass (`weather_get_alerts`) |

## What we learned

- **Running sign-in ourselves is small.** About 450 lines for discovery, registration, PKCE,
  callback and refresh. The library never runs its own sign-in: a small HTTP handler adds our
  stored token to each request.
- **The library's tools slot straight in.** `McpClientTool` is an M.E.AI `AIFunction`. Adding the
  list in `AIAgentFactory` was about 10 lines, and every route that builds an agent gets them.
- **The sample's tool formats were fine on both providers.** This doesn't settle the question
  for real services, whose schemas are messier (see the study's risk list).
- **The dependency bump restored cleanly.** `ModelContextProtocol.Core` 2.4.0 pulled
  `Microsoft.Extensions.AI.Abstractions` up to 10.11.0 next to `Microsoft.Extensions.AI` 10.7.0
  with no errors, and all 432 Agent tests pass. The real work still needs the floors raised on
  purpose.
- **New gap: retry after a rejected token.** If the server rejects a token (401) that we think is
  valid, we skip the server for that turn. The real feature should refresh once and retry. Found
  when the test sign-in server restarted with a new signing key.
- **New gap: a forgotten client registration.** We keep the registered client ID. If the service
  forgets it, sign-in fails until the stored registration is cleared. The real feature should
  re-register when the service rejects the client.

## What the POC skips on purpose

Admin UI (servers come from `appsettings`), database tables (it uses Umbraco's key/value table),
a database lock around refresh (process-local only), per-call audit, source labels, approvals,
personal sign-in, Deploy, v17, and the Copilot UI (tested through `IAIAgentService.RunAgentAsync`).

## Code

| File | What it does |
| --- | --- |
| `Umbraco.AI.Agent.Core/McpPoc/McpPocSignInService.cs` | Discovery, registration, PKCE, callback, refresh. Encrypted with `IAISensitiveFieldProtector` |
| `Umbraco.AI.Agent.Core/McpPoc/McpPocToolSource.cs` | Lists allowlisted tools from signed-in servers, prefixes names, skips servers that fail |
| `Umbraco.AI.Agent.Core/Chat/AIAgentFactory.cs` | Adds the MCP tools to every agent run |
| `Umbraco.AI.Agent.Web/Api/Management/McpPoc/McpPocController.cs` | `GET servers`, `POST servers/{alias}/connect` (backoffice users only) |
| `Umbraco.AI.Agent.Web/Api/Management/McpPoc/McpPocCallbackController.cs` | Anonymous OAuth return page at `/umbraco/ai/mcp-poc/callback` |

## How to run it

1. Clone the SDK samples (once): `git clone --depth 1 --branch v2.4.0
   https://github.com/modelcontextprotocol/csharp-sdk.git demos/mcp/csharp-sdk`, then change
   `samples/ProtectedMcpServer/ProtectedMcpServer.csproj` to `net10.0`.
2. Start the test sign-in server:
   `cd demos/mcp/csharp-sdk/tests/ModelContextProtocol.TestOAuthServer && dotnet run -f net10.0 --launch-profile https`
   (`https://localhost:7029`).
3. Start the weather MCP server: `cd demos/mcp/csharp-sdk/samples/ProtectedMcpServer && dotnet run`
   (`http://localhost:7071`).
4. Add the server to `demos/v18/Umbraco.AI.DemoSite/appsettings.Development.json`:

    ```json
    "Umbraco": { "AI": { "Agent": { "McpPoc": { "Servers": [ {
      "Alias": "weather", "Name": "Weather (MCP sample server)", "Url": "http://localhost:7071/",
      "AllowedTools": [ "get_alerts", "get_forecast" ] } ] } } } }
    ```

5. Start the demo site and sign in to the backoffice. In the browser console, run:

    ```js
    const r = await fetch('/umbraco/ai/management/api/v1/mcp-poc/servers/weather/connect',
      { method: 'POST', headers: { Authorization: 'Bearer [redacted]' } });
    location.href = (await r.json()).authorizationUrl;
    ```

    The test server approves right away and the page says "Connected".
6. Ask any agent about weather alerts in a US state. The tool is named `weather_get_alerts`.
