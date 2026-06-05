# Telemetry

Umbraco.AI contributes anonymous usage information to the standard
[Umbraco CMS telemetry](https://docs.umbraco.com/umbraco-cms/fundamentals/backoffice/settings-dashboards)
system, the same way Umbraco Commerce and other Umbraco products do. There is no separate
Umbraco.AI reporting endpoint — data travels in the CMS's single daily, anonymized telemetry
report, identified only by a random per-installation GUID.

## What is collected

Data is only included when the site's telemetry level (Settings → Telemetry Data) is set to
**Detailed**. Everything reported is an aggregate count, a boolean, or a normalized
identifier — never content, names, or identities.

### Umbraco.AI (core)

| Data | Example |
| --- | --- |
| Installed AI provider IDs (official providers only; custom providers are counted, never named) | `openai`, `anthropic` + custom count |
| Provider IDs with at least one connection (official only + custom count) | `openai` |
| Connection count | `3` |
| Profile counts (total and per capability) | `5` total, `3` Chat, `2` Embedding |
| Context and guardrail counts | `4`, `2` |
| Context picker adoption (data type count, referencing content type count, whether any content has saved values) | `1`, `3`, `true` |
| Guardrail evaluator IDs in use (built-in only; custom evaluators are counted, never named) | `regex`, `pii` + custom count |
| Test count, test run count, and test feature/grader IDs in use (built-in only; custom ones are counted, never named) | `6`, `120`, `prompt`, `contains` + custom counts |
| Registered tool counts (built-in and custom — tool names/IDs are never sent) | `12` total, `3` custom |
| Registered context resource type counts (total and custom) | `4`, `1` |
| Custom extension registration counts per extension point (middleware, tool scopes, entity adapters, resolvers, workflows, …) | `1` chat middleware, `0` workflows |
| Which capabilities have a default profile configured | `Chat`, `Embedding` |
| Whether audit logging / usage analytics are enabled | `true` |
| Requests in the last 30 days and success rate (total and per capability) | `1250`, `0.98`, `1100` Chat |

### Add-ons (when installed)

| Package | Data |
| --- | --- |
| Umbraco.AI.Prompt | Prompt counts (total, active, with profile/context/guardrail), display modes in use, prompt executions in the last 30 days |
| Umbraco.AI.Agent | Agent counts (total, active, per type, with profile/guardrail), built-in surface IDs in use + custom surface count, agent executions in the last 30 days |
| Umbraco.AI.Search | Vector entry count |

## What is never collected

- Prompt instructions, system messages, chat content, or AI responses
- API keys, endpoints, or any connection settings
- Names or aliases of profiles, prompts, agents, connections, contexts, or guardrails
- IDs of custom code extensions (tools, evaluators, graders, test features, surfaces,
  resource types, middleware) — only extensions shipped in official Umbraco.AI packages are
  ever named; everything else (including community packages) appears solely as counts
- Model or deployment names — model IDs are not reported at all, as they can be
  user-authored (e.g. Azure AI Foundry deployment names)
- Token usage totals
- User identities, entity IDs, or content references
- Error messages

## Opting out

Umbraco.AI telemetry respects the CMS telemetry level: set it to **Basic** or **Minimal** in
the backoffice (Settings → Telemetry Data) and no detailed Umbraco.AI data is sent. At the
Basic level the installed `Umbraco.AI.*` package names and versions still appear in the CMS
package list.

To switch off Umbraco.AI's contribution specifically — without lowering the site-wide
telemetry level — use:

```json
{
    "Umbraco": {
        "AI": {
            "Telemetry": {
                "Enabled": false
            }
        }
    }
}
```
