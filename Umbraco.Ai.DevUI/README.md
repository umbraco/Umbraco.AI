# Umbraco.Ai.DevUI

DevUI integration for Umbraco.Ai, providing a development interface for working with AI agents.

## Overview

Umbraco.Ai.DevUI integrates Microsoft's DevUI with Umbraco.Ai, providing:
- Visual interface for discovering and interacting with AI agents
- Runtime agent discovery (agents created in Umbraco appear automatically)
- Support for both framework-registered agents and Umbraco.Ai agents
- Entity metadata and tool inspection

## Installation

```bash
dotnet add package Umbraco.Ai.DevUI
```

## Configuration

The DevUI package automatically registers itself via the `DevUIComposer`. Services and endpoints are registered automatically using Umbraco's pipeline filter system.

**IMPORTANT: DevUI only works in Development mode.** No services or endpoints will be registered if the application is running in Production or Staging mode. This means:

- Zero runtime overhead in production environments
- No endpoints exposed accidentally
- Services like `AddDevUI()`, `AddOpenAIResponses()`, and entity discovery are completely skipped

Simply install the package and run your Umbraco site in Development mode. The DevUI interface will be available at `/umbraco/devui`.

### Manual Configuration (Optional)

If you need more control over when DevUI is enabled, you can disable the automatic registration in the composer and manually configure it in your `Program.cs`:

```csharp
using Umbraco.Ai.DevUI.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

if (builder.Environment.IsDevelopment())
{
    // Manually map DevUI endpoints (only if composer is disabled)
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapUmbracoAiDevUI();
    });
}

await app.RunAsync();
```

## Features

### Runtime Agent Discovery

Agents created in Umbraco.Ai are automatically discovered and displayed in DevUI. When you create a new agent in the backoffice, it immediately appears in the DevUI interface without requiring a restart.

### Entity Information

For each agent, DevUI displays:
- Agent name, description, and alias
- Instructions
- Model and provider information
- Available tools
- Chat client type
- Custom metadata

### Endpoints

The package provides the following endpoints:

- `/devui` - DevUI frontend interface
- `/meta` - DevUI metadata endpoint
- `/v1/entities` - List all discoverable entities (agents)
- `/v1/entities/{entityId}/info` - Get detailed information for a specific entity

## Architecture

The package follows standard Umbraco.Ai patterns:

- **Composer** - Automatic registration via `DevUIComposer`
- **Extensions** - Builder and endpoint configuration
- **Services** - Entity discovery and information retrieval
- **Models** - DTOs for DevUI API responses

The DevUI endpoints are registered using Umbraco's `UmbracoPipelineFilter` system, which integrates seamlessly with the Umbraco pipeline.

## Requirements

- .NET 10.0
- Umbraco CMS 17.x
- Umbraco.Ai.Core
- Umbraco.Ai.Agent (for agent discovery)
- Microsoft.Agents.AI.DevUI (1.0.0-preview.260121.1 or higher)
- Microsoft.Agents.AI.Hosting (1.0.0-preview.260121.1 or higher)

## Security

### Development Mode Only

**DevUI is only available in Development mode.** The package checks `IHostEnvironment.IsDevelopment()` and will not register endpoints if the application is running in Production or Staging environments.

To enable Development mode, set the `ASPNETCORE_ENVIRONMENT` or `DOTNET_ENVIRONMENT` environment variable to `Development`.

### Authentication Required

**All DevUI endpoints require backoffice authentication.** Users must be logged into the Umbraco backoffice to access:

- `/devui` - The DevUI frontend interface
- `/meta` - DevUI metadata API
- `/v1/entities` - Entity discovery endpoints

The package uses Umbraco's `BackOfficeAccess` authorization policy, which means:
- Users must be authenticated as Umbraco backoffice users
- Standard Umbraco security policies apply
- Unauthorized requests receive a 401 Unauthorized response

### Security Considerations

While DevUI requires authentication, be aware that it:

- Exposes agent metadata including instructions, tools, and model information
- Provides an interactive interface for testing and interacting with agents
- Reveals the structure and capabilities of your AI agents

**Built-in Protection:**
- DevUI automatically disables itself outside of Development mode
- No manual configuration needed to prevent production exposure
- Endpoints return 404 if accessed in non-Development environments

**Recommendations:**
- Always verify your environment configuration before deployment
- Review what agents and information are exposed during development
- Consider using separate agent configurations for different environments
- Be cautious about committing sensitive agent instructions to source control

## License

See the LICENSE file in the repository root.
