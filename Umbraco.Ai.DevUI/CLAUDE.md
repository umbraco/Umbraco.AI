# CLAUDE.md - Umbraco.Ai.DevUI

This file provides guidance to Claude Code when working with the Umbraco.Ai.DevUI package.

## Overview

Umbraco.Ai.DevUI integrates Microsoft's DevUI with Umbraco.Ai, providing a development interface for AI agents.

## Project Structure

This package uses a simplified structure suitable for a developer tool:

```
Umbraco.Ai.DevUI/
├── src/
│   └── Umbraco.Ai.DevUI/
│       ├── Extensions/              # Builder and endpoint extensions
│       ├── Services/                # Entity discovery services
│       ├── Models/                  # DTOs for DevUI API
│       └── DevUIComposer.cs        # Umbraco Composer
├── Umbraco.Ai.DevUI.sln
├── README.md
└── CLAUDE.md
```

## Key Concepts

### DevUI Integration

DevUI is Microsoft's development interface for AI agents. We integrate it with Umbraco.Ai by:

1. **Service Registration** - `AddUmbracoAiDevUI()` extension registers DevUI services and our custom entity discovery
2. **Endpoint Mapping** - `MapUmbracoAiDevUI()` maps the DevUI frontend, meta endpoint, and custom entity discovery endpoints
3. **Reflection-Based Mapping** - Uses reflection to call internal DevUI methods for mapping the frontend and meta API

### Runtime Agent Discovery

The key feature is runtime discovery of Umbraco.Ai agents. The `DevUIEntityDiscoveryService`:

- Queries the database for agents using `IAiAgentService`
- Combines framework-registered agents with Umbraco.Ai agents
- Extracts metadata from agents (tools, model info, provider details)
- Provides endpoints that override DevUI's default entity discovery

### Keyed Service Resolution

We register a keyed service factory that resolves AIAgent instances by alias:

```csharp
builder.Services.AddKeyedSingleton<AIAgent>(KeyedService.AnyKey, (sp, key) =>
{
    // Look up agent by alias and create MAF agent instance
});
```

This allows DevUI to interact with Umbraco.Ai agents using their aliases.

## Dependencies

- **Umbraco.Ai.Core** - Core AI functionality
- **Umbraco.Ai.Agent.Core** - Agent management (required for agent discovery)
- **Microsoft.Agents.AI.DevUI** - Microsoft's DevUI package
- **Microsoft.Agents.AI.Hosting** - Hosting infrastructure for DevUI

## Coding Standards

Follow the standards defined in the root CLAUDE.md:

- Async methods: `[Action][Entity]Async` pattern
- Services encapsulate business logic
- Extension methods in `Umbraco.Ai.DevUI.Extensions` namespace

## Security

### Development Mode Only

DevUI completely disables itself when not running in Development mode:

- **Early Environment Check**: The `AddUmbracoAiDevUI()` extension method checks `IHostEnvironment.IsDevelopment()` before registering any services
- **Zero Registration**: If not in Development mode, the method returns immediately without registering services, middleware, or endpoints
- **No Overhead**: This means zero runtime overhead in Production/Staging environments

The environment check uses a temporary service provider during registration, which is properly disposed. This is a known pattern when environment-based registration decisions are needed.

### Authentication

All DevUI endpoints require backoffice authentication (when running in Development mode):

- **Middleware**: `DevUIAuthorizationMiddleware` checks authentication for `/umbraco/devui` and `/meta` paths
  - Registered in `postPipeline` callback (runs AFTER `UseAuthentication()`)
  - This ensures `context.User` is populated from the authentication cookie before we check it
- **Endpoint Authorization**: Custom entity discovery endpoints (`/v1/entities`) use `.RequireAuthorization(AuthorizationPolicies.BackOfficeAccess)`
- **Policy**: Uses Umbraco's `BackOfficeAccess` authorization policy

**Important**: The middleware must use `postPipeline` (not `postRouting`) because Umbraco's middleware order is:
1. `PostRouting` callbacks run
2. `UseAuthentication()` processes cookies
3. `UseAuthorization()` checks authorization
4. `PostPipeline` callbacks run ← Middleware runs here

If the middleware ran in `postRouting`, it would execute before authentication and `context.User` would not be set yet.

## Development Notes

### Testing

Test locally by:
1. Ensure `ASPNETCORE_ENVIRONMENT` or `DOTNET_ENVIRONMENT` is set to `Development`
2. Running the demo site in development mode
3. Logging into the Umbraco backoffice
4. Navigating to `/umbraco/devui` (you should be authenticated)
5. Verifying that `/v1/entities` returns agent data (not 401)
6. Creating agents in the Umbraco backoffice
7. Verifying agents appear in DevUI without restart
8. Testing that unauthenticated requests receive 401 Unauthorized or redirect to login
9. Testing that DevUI is not available when environment is changed to Production

### Reflection Usage

The package uses reflection to call internal DevUI methods (`MapDevUI`, `MapMeta`). This is necessary because these methods are not public. If DevUI's internal API changes, this may need updates.

### JSON Serialization

DevUI expects snake_case JSON property names. Use `DevUIJsonSerializerOptions.Options` for all DevUI API responses.

## Common Tasks

### Adding New Entity Metadata

To add new metadata fields to entity discovery:

1. Add property to `DevUIEntityInfo` record
2. Update `CreateEntityInfoFromFrameworkAgent` or `CreateEntityInfoFromUmbracoAgentAsync` in `DevUIEntityDiscoveryService`
3. Extract the metadata from the agent instance

### Supporting Additional Entity Types

Currently only agents are supported. To add workflows or other entity types:

1. Update `GetAllEntitiesAsync` to query additional entity sources
2. Create helper methods to convert entities to `DevUIEntityInfo`
3. Update entity type filtering logic if needed

## Package Publishing

This package is safe to include in production deployments because:

- DevUI automatically disables itself completely outside of Development mode
- No services, middleware, or endpoints are registered when `IHostEnvironment.IsDevelopment()` returns false
- Zero runtime overhead in Production/Staging environments
- The package can be installed but will remain completely inactive

The composer will still run (as all composers do), but the `AddUmbracoAiDevUI()` method returns immediately without registering anything if not in Development mode.
