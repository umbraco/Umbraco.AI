## Umbraco.AI.Cloudflare

Cloudflare AI provider for Umbraco.AI — connect Cloudflare's Clairic model to read typed values out of
free text, without prompting a chat model and parsing the reply.

### Features

- **Extraction capability** - Read text, number, date, time, duration and list values out of the same
  text in a single call
- **Clairic model** - A single model (`@cf/cloudflare/clairic`) tuned for extraction
- **Custom Endpoints** - Support for proxy servers or alternative endpoints

### Requirements

- Umbraco CMS 18.0.0+
- Umbraco.AI 18.0.0+
- .NET 10.0
- Cloudflare account ID and API token
- The `Umbraco:AI:Experimental:Extraction` feature flag enabled

> **Experimental:** the Extraction capability is marked `[Experimental("UMBRACOAI_EXTRACTION")]` in
> Umbraco.AI.Core. Its shape may change while still being validated.
