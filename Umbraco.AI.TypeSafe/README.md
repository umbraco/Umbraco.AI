## Umbraco.AI.TypeSafe

TypeSafe AI provider for Umbraco.AI — connect Jev, a model purpose-built for typed yes/no, pick-one,
and score decisions, without prompting a chat model and parsing text.

### Features

- **Decision capability** - Ask a yes/no, pick-one (2-255 options), or score (2-10 levels) question and
  get back a typed, probability-scored answer
- **Jev model** - A single model (`jev-latest`) tuned specifically for classification-shaped questions
- **Custom Endpoints** - Support for proxy servers or alternative endpoints

### Requirements

- Umbraco CMS 18.0.0+
- Umbraco.AI 18.0.0+
- .NET 10.0
- TypeSafe AI API key
- The `Umbraco:AI:Experimental:Decision` feature flag enabled

> **Experimental:** the Decision capability is marked `[Experimental("UMBRACOAI_DECISION")]` in
> Umbraco.AI.Core. Its shape may change while still being validated.
