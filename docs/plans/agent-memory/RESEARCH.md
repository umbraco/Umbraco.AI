# Research: Agent memory

What M.E.AI and MAF offer, what other products do, and the public lessons. Gathered
26-11-2026; every claim links its source.

## What we can reuse

- **MAF `AIContextProvider`** is the hook to build on. It runs before each model call (can add
  instructions, messages *and tools*) and after it (for extraction). It's attached through
  `ChatClientAgentOptions.AIContextProviders`, beside the `ChatHistoryProvider` Workspace
  already uses. One instance serves all sessions; per-session state goes in the
  `AgentSession` StateBag.
  ([concept](https://learn.microsoft.com/agent-framework/concepts/agents/conversations/context-providers),
  [API](https://learn.microsoft.com/dotnet/api/microsoft.agents.ai.aicontextprovider?view=agent-framework-dotnet-latest))
- **MAF does not filter what a provider injects.** Microsoft's own note: a compromised memory
  store is an indirect prompt injection. We must label memory as data, not instructions.
- **Built-in providers don't fit:**
  - `ChatHistoryMemoryProvider` stores whole messages in a vector store, not short facts.
    ([docs](https://learn.microsoft.com/agent-framework/concepts/agents/conversations/chat-history-memory-provider))
  - `FileMemoryProvider` is a file model with seven file tools.
    ([API](https://learn.microsoft.com/dotnet/api/microsoft.agents.ai.filememoryprovider?view=agent-framework-dotnet-latest))
  - `FoundryMemoryProvider` is Azure-only and preview.
    ([docs](https://learn.microsoft.com/agent-framework/integrations/by-component/context-providers/microsoft-foundry))
  - Mem0 has no MAF .NET package; its .NET provider lives in Semantic Kernel.
    ([SK](https://learn.microsoft.com/semantic-kernel/frameworks/agent/agent-memory))
- **M.E.AI has no memory abstraction.** `Microsoft.Extensions.VectorData` would only matter if
  we add semantic `recall` later.
  ([MEVD](https://learn.microsoft.com/dotnet/ai/conceptual/mevd-library))
- **Scoping to mirror:** MAF and SK both scope by application, agent, user and session.
  Microsoft's guidance: take the scope from the signed-in identity, never from model output.
  ([Redis provider](https://learn.microsoft.com/agent-framework/integrations/by-component/context-providers/redis))

## What others do

- **ChatGPT:** saved memories are a separate, editable list. Project-only memory keeps project
  chats in and outside chats out; a shared project uses no member's personal memory.
  Temporary chats neither read nor write memory.
  ([FAQ](https://help.openai.com/en/articles/8590148-memory-faq),
  [projects](https://help.openai.com/en/articles/10169521))
- **Claude.ai:** each project has its own memory plus account memory, with a per-project
  "Use account memory" switch. Memories are editable topics; some data (ID numbers, criminal
  history, immigration status) is never stored.
  ([help](https://support.claude.com/articles/11817273-how-does-claude-s-memory-work))
- **Anthropic API memory tool:** file-style `/memories` folder the app stores anywhere; the
  model reads it on demand rather than having it injected.
  ([docs](https://platform.claude.com/docs/en/agents-and-tools/tool-use/memory-tool))
- **Gemini:** "Saved info" added in settings or by asking; tells users when a new memory is
  saved. ([help](https://support.google.com/gemini/answer/16413516))
- **Microsoft 365 Copilot:** saves automatically; memories are findable through eDiscovery, so
  memory is a compliance record.
  ([Learn](https://learn.microsoft.com/microsoft-365/copilot/copilot-personalization-memory))
- **CMS and DXP vendors** (Sitecore, Optimizely, Contentful, Adobe, Contentstack, Storyblok):
  no documented agent memory feature found. Open ground.
- **Letta (MemGPT):** always-in-context memory blocks the agent edits with tools. Closest to
  our "inject all, edit by tool" design. ([docs](https://docs.letta.com/guides/agents/base-tools))
- **LangGraph / LangMem:** memories under namespaces like `("memories", user_id)`; offers both
  a tool and background extraction.
  ([docs](https://docs.langchain.com/oss/python/concepts/memory))

## Public lessons

- **SpAIware (ChatGPT, 2024):** a web page wrote attacker instructions into memory, and every
  later chat leaked data.
  ([Hacker News](https://thehackernews.com/2024/09/chatgpt-macos-flaw-couldve-enabled-long.html))
- **Gemini delayed tool call (2025):** a document said "if the user says X, save Y"; the save
  fired later on an innocent reply. Advice: once untrusted content is in context, sensitive
  tool calls need user confirmation.
  ([write-up](https://embracethered.com/blog/posts/2025/gemini-memory-persistence-prompt-injection))
- **MINJA (2025):** an attacker with query access gets an agent to store poisoned memories
  that later reach *other* users. A direct risk to shared spaces.
  ([paper](https://arxiv.org/abs/2503.03704))

## Published limits

Gathered 26-11-2026. Few products publish any.

- **ChatGPT, Claude.ai, Gemini, Microsoft 365 Copilot:** no published per-memory or total
  limit. ([ChatGPT](https://help.openai.com/en/articles/8590148-memory-faq),
  [Claude](https://support.claude.com/en/articles/11817273),
  [M365](https://learn.microsoft.com/en-us/microsoft-365/copilot/copilot-personalization-memory))
  Users of ChatGPT and M365 report an unannounced "memory full" (forum posts, unofficial).
- **Claude Code:** loads "up to the first 200 lines or 25KB of MEMORY.md".
  ([docs](https://code.claude.com/docs/en/memory)) The only published always-loaded budget.
- **Anthropic API memory tool:** no cap; tells implementers to cap file size, and `view`
  truncates past 16,000 characters.
  ([docs](https://platform.claude.com/docs/en/agents-and-tools/tool-use/memory-tool))
- **Azure Foundry memory (preview):** 10,000 memories per scope, 100 scopes per store.
  ([docs](https://learn.microsoft.com/azure/foundry/agents/concepts/what-is-memory))
- **Retrieval defaults:** MAF `ChatHistoryMemoryProvider` 3 results, Mem0 10-20, LangGraph and
  LangMem 10. These cap what's *loaded*, not what's stored.
- **Letta:** memory blocks are whole profile documents, 2,000-5,000 characters in older
  releases, 20,000-100,000 now
  ([constants.py](https://github.com/letta-ai/letta/blob/0.16.8/letta/constants.py)). A
  different unit from our one-fact memories.

## What it means for us

- Build memory as our own `AIContextProvider`, on our own EF table. No vector store in v1.
- Take every scope key from the signed-in user and the server's conversation, never from tool
  arguments.
- Inject memories as labelled facts ("data, not instructions").
- Keep a never-store list (passwords, keys, ID numbers, health and similar).
- Don't extract memories in the background; tool saves with a visible note are safer.
- Guard saves after untrusted content, and saves to shared spaces (see DECISION-LOG).
- Later: a temporary chat that skips memory, expiry of unused memories, and the Anthropic
  memory tool as a provider-native option.
