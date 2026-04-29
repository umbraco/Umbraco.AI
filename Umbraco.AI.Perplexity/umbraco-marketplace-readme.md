## Umbraco.AI.Perplexity

Perplexity provider for Umbraco.AI - integrate Perplexity's Sonar family of search-augmented chat models, grounding AI responses in current web results.

### Features

- **Sonar Models** - Access Sonar, Sonar Pro, Sonar Reasoning Pro, and Sonar Deep Research
- **Search-Augmented Chat** - Responses grounded in real-time web search
- **OpenAI-Compatible** - Uses Perplexity's OpenAI-compatible chat completions API
- **Dynamic Model Discovery** - Automatically picks up new Sonar variants as Perplexity adds them
- **Custom Endpoints** - Support for proxy servers or alternative endpoints

### Best for

Content generation, summarization, and search-grounded Q&A — anywhere a current, citation-backed answer matters more than tool use.

### Limitations

**Tool / function calling is not supported.** Perplexity's Sonar API has no `tools` parameter — Sonar models perform web search natively as their built-in capability. Use **OpenAI** or **Anthropic** providers for any AI flow in Umbraco that requires tools (e.g., agents, content-aware tools).

### Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- .NET 10.0
- Perplexity API key
