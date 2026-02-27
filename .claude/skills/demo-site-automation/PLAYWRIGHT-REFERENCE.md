# Playwright MCP Tools Reference

Quick reference for Playwright MCP tools used in demo site browser automation.

## Contents

- [Core Tools](#core-tools)
- [Tool Details](#tool-details)
- [Common Patterns](#common-patterns)

## Core Tools

| Tool                      | Purpose               | When to Use                           |
| ------------------------- | --------------------- | ------------------------------------- |
| `browser_navigate`        | Go to URL             | Initial navigation, section changes   |
| `browser_snapshot`        | Capture page state    | Before/after actions, discovery       |
| `browser_click`           | Click element         | Buttons, links, icons                 |
| `browser_fill_form`       | Fill multiple fields  | Login, create/edit forms              |
| `browser_type`            | Type text slowly      | Rich text editors, search boxes       |
| `browser_wait_for`        | Wait for element/text | After navigation, async loading       |
| `browser_take_screenshot` | Visual screenshot     | Debugging, visual confirmation        |
| `browser_evaluate`        | Run JavaScript        | Complex interactions, data extraction |

## Tool Details

### browser_navigate

Navigate to a URL. Use for initial page load or section changes.

```
mcp__playwright__browser_navigate
  url: "https://127.0.0.1:44355/umbraco"
```

### browser_snapshot

Capture accessibility snapshot. Returns element refs for interactions.

```
mcp__playwright__browser_snapshot
```

**Prefer this over screenshot** - faster, provides refs, better for automation.

### browser_click

Click an element by its ref from snapshot.

```
mcp__playwright__browser_click
  ref: "button[5]"
  element: "Create connection button"
```

### browser_fill_form

Fill multiple form fields at once.

```
mcp__playwright__browser_fill_form
  fields: [
    { name: "Email", type: "textbox", ref: "input[1]", value: "admin@example.com" },
    { name: "Password", type: "textbox", ref: "input[2]", value: "password1234" }
  ]
```

### browser_type

Type text character by character. Use for inputs that need key events.

```
mcp__playwright__browser_type
  ref: "textarea[1]"
  text: "System prompt content"
  slowly: true
```

### browser_wait_for

Wait for text to appear or disappear.

```
mcp__playwright__browser_wait_for
  text: "Dashboard"
  time: 3
```

### browser_evaluate

Run JavaScript on the page. Use for complex queries.

```
mcp__playwright__browser_evaluate
  function: "() => document.querySelectorAll('uui-table-row').length"
```

## Common Patterns

### Login Pattern

```
browser_snapshot (check state)
browser_fill_form (email + password)
browser_click (login button)
browser_wait_for (dashboard)
browser_snapshot (confirm)
```

### Create Entity Pattern

```
browser_navigate (to section)
browser_snapshot (find create button)
browser_click (create button)
browser_snapshot (see form)
browser_fill_form (entity fields)
browser_click (save button)
browser_wait_for (success message)
```

### Navigation Pattern

```
browser_navigate (to section URL)
browser_wait_for (section content)
browser_snapshot (verify and discover)
```
