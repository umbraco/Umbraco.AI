---
name: docs-screenshots
description: Capture realistic backoffice screenshots of Umbraco.AI features for Umbraco.Docs or marketing use. Use when asked for product screenshots, doc images, or marketing shots of the AI backoffice UI.
argument-hint: [section or feature to screenshot, e.g. "AI Connections", "Copilot chat"]
---

# Docs & Marketing Screenshots

Capture clean, realistic screenshots of the Umbraco.AI backoffice for documentation (Umbraco.Docs) or marketing use. This has come up repeatedly as an ad-hoc, manual task — this skill exists so it doesn't have to be reinvented each time.

## Why this exists

Past attempts at this task had two recurring problems, both real incidents, not hypothetical:

1. **Placeholder-looking content.** Screenshots populated with "test", "test123", or other obviously-fake data got rejected: *"make them look more real world"*. Always seed realistic-looking demo data before capturing.
2. **An unrelated Xcode license prompt derailed the session.** This happens when a browser tool ends up trying to launch/install **WebKit** on macOS, which requires Xcode command-line tools. Always force **Chromium**, never let the tool default/fall back to WebKit.

## Steps

### 1. Get the demo site running

```bash
/demo-site-management status   # check first
/demo-site-management start    # if not running
```

Use the discovered HTTPS address for all navigation below.

### 2. Use Playwright MCP, Chromium only, isolated

Per the root `CLAUDE.md`, prefer Playwright MCP over the Claude in Chrome extension for demo-site work (the extension shares your real Chrome profile and can log out concurrent sessions). This repo's `.mcp.json` already runs Playwright with `--isolated`.

- If a browser needs to be launched explicitly, make sure it launches **Chromium**, not WebKit. If you ever see an Xcode command-line-tools / license prompt, stop — that means WebKit got selected somewhere. Do not accept the Xcode license or try to work around it; back out and force Chromium instead.
- Set a consistent viewport before capturing anything (e.g. 1440x900) so every screenshot in a set matches — inconsistent sizes across a doc page look sloppy.

### 3. Seed realistic content, not placeholders

Before navigating to the screen you're capturing, make sure whatever entity/list is on screen looks like a real customer's setup, not test fixtures:

- Connection/profile names: real-sounding (e.g. "Production OpenAI", "Marketing Copy Assistant"), not "Test Connection 1".
- Prompt/agent names and descriptions: a real, plausible use case (e.g. "Blog post summarizer" with a real-sounding system prompt), not lorem ipsum or "asdf".
- If `/demo-site-automation` needs to create the entity first, use it (`create-connection`, `create-profile`, `create-prompt`, `create-agent`), then immediately fix up any field the automation filled with a generic placeholder.
- Prefer content that matches what the doc page or marketing copy is actually about — a screenshot of an "Anthropic" connection next to text about Claude reads better than a random unrelated provider.

### 4. Capture

Use `mcp__playwright__browser_take_screenshot` (or `browser_snapshot` first to confirm the page looks right before spending a screenshot on it). Crop to the relevant panel when the doc only needs one section, not the whole backoffice chrome, unless the full chrome is the point (e.g. a "getting started" overview shot).

Save screenshots with a clear name indicating the feature and date, e.g. `ai-connections-list-2026-09.png`, so they're easy to find later and easy to tell apart from a stale previous capture.

### 5. Review before handing off

Before saying the screenshots are ready:

- Skim every one for leftover placeholder text, lorem ipsum, or obviously-fake data.
- Confirm the theme (light/dark) matches what was asked — default to light unless told otherwise.
- Confirm nothing sensitive is visible (real API keys, real customer data) — this should never come up on the demo site, but double-check anyway before handing screenshots off for external use.

## Known-good vs uncertain

- **Confirmed**: Chromium works for this without any native-tooling prompts. Playwright MCP + `--isolated` is the safe path.
- **Uncertain**: which exact code path triggers the WebKit/Xcode prompt (browser auto-selection logic, a specific tool call) was never root-caused, only worked around by avoiding WebKit entirely. If it happens again, note what triggered it so this can be tightened.
