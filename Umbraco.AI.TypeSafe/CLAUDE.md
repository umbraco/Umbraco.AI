# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.TypeSafe provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
dotnet build Umbraco.AI.TypeSafe.slnx
dotnet test Umbraco.AI.TypeSafe.slnx
```

## Architecture Overview

Umbraco.AI.TypeSafe is a provider plugin for Umbraco.AI that connects TypeSafe AI's "Jev" model — a
model purpose-built for typed decisions (yes/no, pick-one, score) rather than open-ended chat. It
exposes only the experimental Decision capability; there is no chat or embedding model to wrap.

TypeSafe has no official .NET SDK, so this provider calls Jev directly over HTTP
(`IHttpClientFactory` + `System.Text.Json`), the same approach `Umbraco.AI.FireworksAI` uses for its
native models endpoint.

### Project Structure

| Project                | Purpose                                              |
| ----------------------- | ---------------------------------------------------- |
| `Umbraco.AI.TypeSafe` | Provider, decision capability, client, and settings   |

This provider has a real test project (`tests/Umbraco.AI.TypeSafe.Tests.Unit`), which is a deliberate
deviation from the "providers have no test project" convention: unlike an SDK-backed provider, this
provider's client *is* the wire mapping (hand-written HTTP/JSON), so that mapping needs its own tests.

### Provider Implementation

```csharp
[AIProvider("typesafe", "TypeSafe AI")]
public class TypeSafeProvider : AIProviderBase<TypeSafeProviderSettings>
{
    public TypeSafeProvider(IAIProviderInfrastructure infrastructure, IHttpClientFactory httpClientFactory)
        : base(infrastructure)
    {
        WithCapability<TypeSafeDecisionCapability>();
    }
}
```

### Capability

**Decision** (`TypeSafeDecisionCapability`) extends `AIDecisionCapabilityBase<TypeSafeProviderSettings>`.
Model list is a static `["jev-latest"]` — Jev documents no models endpoint. `CreateClientAsync` builds a
`TypeSafeDecisionClient` over an `HttpClient` from `IHttpClientFactory`; every request carries the API
key and endpoint from `TypeSafeProviderSettings` directly rather than a preconfigured `HttpClient`.

Every file touching `IAIDecisionClient`/`AIDecisionCapabilityBase` carries a per-file
`#pragma warning disable UMBRACOAI_DECISION`, matching `OpenAIProvider.cs`
(`UMBRACOAI_IMAGEGEN`) — never a project-wide `NoWarn`.

### Settings

```csharp
public class TypeSafeProviderSettings
{
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    [AIField]
    public string? Endpoint { get; set; } = "https://api.typesafe.ai";
}
```

### Wire shape (`TypeSafeDecisionClient`)

`POST {Endpoint}/v1/systemone` with `Authorization: Bearer <ApiKey>`, one question keyed `"q"`. See
`docs/plans/decision-capability-release/SPEC.md` "Provider: Umbraco.AI.TypeSafe" for the full request
and response mapping (binary/choice/score, retry policy). The wire mapping itself lands in a later task;
`TypeSafeDecisionClient.AskAsync` currently throws `NotImplementedException`.

## Key Namespaces

- `Umbraco.AI.TypeSafe` - Provider, capability, client, and settings

## Dependencies

- Umbraco CMS 18.x
- Umbraco.AI 18.x

## Target Framework

- .NET 10.0 (`net10.0`)
- Uses Central Package Management (`Directory.Packages.props`)
- Nullable reference types enabled

## Provider Discovery

Auto-discovered via `[AIProvider("typesafe", "TypeSafe AI")]` and assembly scanning during Umbraco
startup.
