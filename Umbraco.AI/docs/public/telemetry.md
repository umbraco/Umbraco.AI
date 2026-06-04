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
| Installed AI provider IDs | `openai`, `anthropic` |
| Provider IDs with at least one connection | `openai` |
| Connection count | `3` |
| Profile counts (total and per capability) | `5` total, `3` Chat, `2` Embedding |
| Model families in use (normalized to public model names) | `openai/gpt-4o`, `anthropic/claude-sonnet` |
| Context and guardrail counts | `4`, `2` |
| Which capabilities have a default profile configured | `Chat`, `Embedding` |
| Whether audit logging / usage analytics are enabled | `true` |
| Requests in the last 30 days and success rate | `1250`, `0.98` |

### Add-ons (when installed)

| Package | Data |
| --- | --- |
| Umbraco.AI.Prompt | Prompt counts (total, active, with profile/context/guardrail), display modes in use |
| Umbraco.AI.Agent | Agent counts (total, active, per type, with profile/guardrail), surface IDs in use |
| Umbraco.AI.Search | Vector entry count |

## What is never collected

- Prompt instructions, system messages, chat content, or AI responses
- API keys, endpoints, or any connection settings
- Names or aliases of profiles, prompts, agents, connections, contexts, or guardrails
- Custom model or deployment names — model IDs are normalized to well-known public model
  families (e.g. `gpt-4o`); anything custom is reported only as `other`
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
