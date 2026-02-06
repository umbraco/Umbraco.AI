# Integration Philosophy

## Building on Standards

Umbraco.AI is built on [Microsoft.Extensions.AI](https://learn.microsoft.com/en-us/dotnet/ai/) (M.E.AI), Microsoft's official abstraction layer for AI services. Rather than creating proprietary interfaces, we chose to embrace this industry standard and add Umbraco-specific value on top.

This "thin layer" approach means Umbraco.AI enhances M.E.AI without hiding or replacing it. You get the benefits of Umbraco integration while retaining full access to the underlying M.E.AI capabilities when you need them.

---

## Why Microsoft.Extensions.AI?

### Industry Standard

M.E.AI is Microsoft's official answer to AI service abstraction in .NET. By building on it, Umbraco.AI aligns with where the .NET ecosystem is heading rather than creating yet another proprietary abstraction that developers need to learn.

### Ecosystem Momentum

The M.E.AI ecosystem is growing rapidly. Major AI providers—OpenAI, Azure OpenAI, Anthropic, Ollama, and others—are building native M.E.AI support. When a new provider adds M.E.AI compatibility, it can work with Umbraco.AI with minimal effort.

### Familiar Patterns

If you've used `Microsoft.Extensions.Logging`, `Microsoft.Extensions.Caching`, or `Microsoft.Extensions.DependencyInjection`, you already understand the patterns M.E.AI follows. The same builder patterns, options configuration, and dependency injection approaches apply. There's less to learn.

### Active Development

Microsoft is actively investing in M.E.AI as part of their broader .NET AI strategy. This means ongoing improvements, bug fixes, and new features that Umbraco.AI automatically benefits from.

---

## The Thin Layer Philosophy

Umbraco.AI follows a deliberate architectural principle: **wrap, don't replace**.

### No Proprietary Types

Umbraco.AI uses M.E.AI's types directly. When you work with chat messages, you're working with `ChatMessage`. When you receive responses, you get `ChatResponse`. There are no Umbraco-specific equivalents that you need to convert to or from.

This means:

- Documentation and examples for M.E.AI apply directly to Umbraco.AI
- Libraries built for M.E.AI work with Umbraco.AI
- Skills you develop transfer outside of Umbraco contexts

### Direct Access When Needed

Umbraco.AI's services provide convenient access through profiles and managed configuration. But when you need the raw `IChatClient` or `IEmbeddingGenerator`, you can get it:

The high-level `IAIChatService` handles profile resolution and configuration for you. For advanced scenarios, you can request the underlying M.E.AI client and work with it directly.

### Transparent Middleware

The middleware pipeline wraps M.E.AI clients using the same builder pattern M.E.AI itself provides. Middleware adds behavior (logging, caching, rate limiting) without changing the interface. What goes in and comes out is still standard M.E.AI.

---

## What Umbraco.AI Adds

While keeping the M.E.AI foundation intact, Umbraco.AI provides the management and integration layer that makes AI practical in Umbraco projects.

**Configuration Management**
Connections store credentials securely. Profiles combine connections with model settings for specific use cases. Administrators can manage these through the backoffice without touching code. See [Core Concepts](core-concepts.md) for details on how these work together.

**Umbraco Integration**
Umbraco.AI plugs into Umbraco's architecture naturally—composers for registration, dependency injection for services, and backoffice UI for administration. It feels like part of Umbraco, not a bolt-on.

**Extensible Pipeline**
Middleware lets you add cross-cutting concerns (logging, caching, content filtering) without modifying your application code. The pipeline architecture means these concerns compose cleanly.

---

## Architecture

```
┌─────────────────────────────────────────────────┐
│           Your Umbraco Application              │
├─────────────────────────────────────────────────┤
│              Umbraco.AI                         │
│    Profiles · Connections · Middleware · UI     │
├─────────────────────────────────────────────────┤
│         Microsoft.Extensions.AI                 │
│      IChatClient · IEmbeddingGenerator          │
├─────────────────────────────────────────────────┤
│           Provider Implementations              │
│       OpenAI · Azure · Anthropic · etc.         │
└─────────────────────────────────────────────────┘
```

Each layer has a clear responsibility:

- **Your application** focuses on what you want AI to do
- **Umbraco.AI** handles configuration, management, and Umbraco integration
- **M.E.AI** provides the standard interface for AI operations
- **Providers** implement the actual AI service communication

---

## Future-Proofing

Building on M.E.AI means Umbraco.AI grows with the ecosystem:

**New Providers**
When a new AI provider releases M.E.AI support, creating an Umbraco.AI provider package is straightforward. The core abstractions are already in place.

**New Capabilities**
As M.E.AI adds support for new AI capabilities (tool calling, structured outputs, multimodal content), Umbraco.AI can expose these through the same patterns.

**Microsoft's Investment**
M.E.AI is part of Microsoft's long-term .NET AI strategy. Improvements to the core abstraction benefit everyone building on it, including Umbraco.AI.

---

## When to Use What

**Use Umbraco.AI services** for most development. The `IAIChatService` and related services handle profile resolution, connection management, and middleware automatically. This is the recommended path for Umbraco projects.

**Use M.E.AI directly** when you need capabilities not yet exposed through Umbraco.AI, when building provider packages, or when working in contexts outside Umbraco. You can always get the underlying `IChatClient` when needed.

---

## Related Documentation

- [Core Concepts](core-concepts.md) — Providers, Connections, Profiles, and Middleware
- [Capabilities](capabilities-feature.md) — Chat, Embedding, and planned capabilities
- [Microsoft.Extensions.AI Documentation](https://learn.microsoft.com/en-us/dotnet/ai/) — Official M.E.AI reference
