# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.Cloudflare provider package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
dotnet build Umbraco.AI.Cloudflare.slnx
dotnet test Umbraco.AI.Cloudflare.slnx
```

## Architecture Overview

Umbraco.AI.Cloudflare is a provider plugin for Umbraco.AI that connects Cloudflare Workers AI. It exposes
a Chat capability (any Text Generation model) and the experimental Extraction capability (Cloudflare's
Clairic model, purpose-built for extracting typed values from text).

**Chat** (`CloudflareChatCapability`) uses the official OpenAI client against Workers AI's OpenAI-compatible
route, `{Endpoint}/accounts/{AccountId}/ai/v1`, authenticated with the Cloudflare API token. Cloudflare has no
`/models` route there, so the model list comes from the account's model search
(`ai/models/search?task=Text Generation`, paged and cached for an hour) in `CloudflareProvider.GetChatModelIdsAsync`.
A failed lookup throws with Cloudflare's message, so "Test connection" reports why.

Model search, the connection check and Extraction call Cloudflare directly over HTTP (`IHttpClientFactory` +
`System.Text.Json`), the same approach `Umbraco.AI.TypeSafe` uses.

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

- Umbraco CMS 17.x
- Umbraco.AI 17.x

## Provider Discovery

Auto-discovered via `[AIProvider("cloudflare", "Cloudflare")]` and assembly scanning during Umbraco startup.
