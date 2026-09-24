# Triage Log

| ID | Title | Type | Date | Outcome |
|----|-------|------|------|---------|
| #413 | Tools and tool scopes without a localization entry show raw keys (uaiTool_…Label) instead of falling back to their name | Bug | 24-09-2026 | Fixed: PRs #417 (v18/dev) + #418 (v17/dev) merged 24-09-2026, incl. surface picker + missing translations. |
| #414 | Chat silently produces no response when a thinking model's reply is truncated at max_tokens (default appears to be 1024) | Bug | 24-09-2026 | Fixed: PRs #422 (v18/dev) + #423 (v17/dev) merged 24-09-2026. Anthropic default 8192 (capped by model limit), RUN_ERROR on any Length finish, OpenAI Responses incomplete mapped to Length. |
