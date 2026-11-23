## Umbraco.AI.Cloudflare

Cloudflare AI provider for Umbraco.AI — chat with the models running on Cloudflare Workers AI, and use
Cloudflare's Clairic model to read typed values out of free text without prompting a chat model and
parsing the reply.

### Features

- **Chat capability** - Chat with any Workers AI Text Generation model through Cloudflare's
  OpenAI-compatible endpoint. The model list is read from your account's model search, so the model
  picker is populated (Cloudflare has no `/models` route on its OpenAI-compatible API). Tool calling
  depends on the model you choose; pick one that supports function calling to use agents and Copilot.
- **Extraction capability** - Read text, number, date, time, duration and list values out of the same
  text in a single call
- **Clairic model** - A single model (`@cf/cloudflare/clairic`) tuned for extraction
- **Custom Endpoints** - Support for proxy servers or alternative endpoints

### Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 17.0.0+
- .NET 10.0
- Cloudflare account ID and API token
- For the Extraction capability only: the `Umbraco:AI:Experimental:Extraction` feature flag enabled

> **Experimental:** the Extraction capability is marked `[Experimental("UMBRACOAI_EXTRACTION")]` in
> Umbraco.AI.Core. Its shape may change while still being validated.
