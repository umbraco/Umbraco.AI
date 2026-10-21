# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.Cloudflare provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
dotnet build Umbraco.AI.Cloudflare.slnx
dotnet test Umbraco.AI.Cloudflare.slnx
```

## Architecture Overview

Umbraco.AI.Cloudflare is a provider plugin for Umbraco.AI that connects Cloudflare's Clairic model, a
model purpose-built for extracting typed values from text. It exposes only the experimental Extraction
capability; there is no chat or embedding model to wrap.

There is no SDK involved: the provider calls Cloudflare directly over HTTP (`IHttpClientFactory` +
`System.Text.Json`), the same approach `Umbraco.AI.TypeSafe` uses. It is named after the company so
other Cloudflare capabilities can be added later.

### Project Structure

| Project                 | Purpose                                               |
| ----------------------- | ----------------------------------------------------- |
| `Umbraco.AI.Cloudflare` | Provider, extraction capability, client, and settings |

The test project (`tests/Umbraco.AI.Cloudflare.Tests.Unit`) fakes only the `HttpMessageHandler`, since
the client is hand-written wire mapping. It is listed in the root `Umbraco.AI.slnx`, which is what CI runs.

### Capability

**Extraction** (`CloudflareExtractionCapability`) extends `AIExtractionCapabilityBase<CloudflareProviderSettings>`.
The model list is a static `["@cf/cloudflare/clairic"]`, because Cloudflare's model search cannot tell
Clairic apart from other models. Every file touching the extraction types carries a per-file
`#pragma warning disable UMBRACOAI_EXTRACTION`, never a project-wide `NoWarn`.

### Settings

`AccountId` (required), `ApiToken` (required, `IsSensitive = true`), `Endpoint` (default
`https://api.cloudflare.com/client/v4`).

## Dependencies

- Umbraco CMS 18.x
- Umbraco.AI 18.x

## Provider Discovery

Auto-discovered via `[AIProvider("cloudflare", "Cloudflare")]` and assembly scanning during Umbraco startup.
