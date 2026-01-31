---
name: demo-site-ix
description: Browser-based interactions with Umbraco.Ai demo site using Playwright. Handles login, navigation, and editing of AI entities (connections, profiles, prompts, agents).
argument-hint: [login|navigate-to-<section>|edit-<entity>|create-<entity>|status]
allowed-tools: mcp__playwright__*, Bash, Read, Skill
---

# Demo Site Browser Interactions

Automate browser-based interactions with the Umbraco.Ai demo site using Playwright MCP.

## Command: $ARGUMENTS

Execute the requested browser interaction with the demo site.

## Available Commands

### Authentication
- **login**: Login to Umbraco backoffice with demo credentials

### Navigation
- **navigate-to-connections**: Navigate to AI Connections section
- **navigate-to-profiles**: Navigate to AI Profiles section
- **navigate-to-prompts**: Navigate to AI Prompts section (Umbraco.Ai.Prompt)
- **navigate-to-agents**: Navigate to AI Agents section (Umbraco.Ai.Agent)
- **navigate-to-copilot**: Open Copilot chat UI (Umbraco.Ai.Agent.Copilot)

### Entity Operations
- **create-connection [provider]**: Create new AI connection (e.g., OpenAI, Anthropic)
- **edit-connection [name]**: Edit existing connection by name
- **create-profile [capability]**: Create new AI profile (e.g., chat, embedding)
- **edit-profile [name]**: Edit existing profile by name
- **create-prompt**: Create new prompt template
- **edit-prompt [name]**: Edit existing prompt by name
- **create-agent**: Create new AI agent
- **edit-agent [name]**: Edit existing agent by name

### Utilities
- **status**: Check browser connection and demo site availability
- **snapshot**: Take accessibility snapshot of current page

## Demo Site Credentials

- **Email**: admin@example.com
- **Password**: password1234
- **Base URL**: Discovered via demo-site skill or default to https://localhost:44355

## Implementation Guide

### Step 1: Discover Demo Site URL

Before any browser interaction, discover the demo site URL:

```bash
# Use demo-site skill to check status and get URL
/demo-site status
```

If demo site is not running, start it first:
```bash
/demo-site start
```

Extract the URL from the status output (format: `https://127.0.0.1:<port>` or `https://localhost:44355`).

### Step 2: Initialize Browser (if needed)

Check if browser page is already open:
```
mcp__playwright__browser_snapshot
```

If error indicates no page, navigate to the demo site:
```
mcp__playwright__browser_navigate: url = <discovered-url>/umbraco
```

### Step 3: Execute Command

#### For "login"

1. Navigate to `/umbraco` if not already there
2. Take snapshot to see current state
3. If already logged in (dashboard visible), report and exit
4. If login form visible:
   - Fill email field: `admin@example.com`
   - Fill password field: `password1234`
   - Click login button
5. Wait for dashboard to load (2-3 seconds)
6. Take snapshot to confirm login success
7. Report: "Logged in successfully as admin@example.com"

**Playwright sequence:**
```
browser_snapshot (check state)
browser_fill_form (email + password fields)
browser_click (login button ref)
browser_wait_for (dashboard content)
browser_snapshot (confirm)
```

#### For "navigate-to-connections"

1. Ensure logged in (check snapshot for dashboard)
2. Navigate to `/umbraco#/ai/connections`
3. Wait 2 seconds for section to load
4. Take snapshot to show connections list
5. Report: "Navigated to AI Connections. Found X connections."

**Umbraco section URLs (all under Settings section):**
- Connections: `/umbraco/section/settings/workspace/uai:connection-root`
- Profiles: `/umbraco/section/settings/workspace/uai:profile-root`
- Prompts: `/umbraco/section/settings/workspace/uai:prompt-root`
- Agents: `/umbraco/section/settings/workspace/uai:agent-root`
- Contexts: `/umbraco/section/settings/workspace/uai:context-root`
- Analytics: `/umbraco/section/settings/workspace/ai-analytics-root`
- Logs: `/umbraco/section/settings/workspace/uai:trace-root`
- AI Settings: `/umbraco/section/settings/workspace/uai:settings-root`
- Copilot: Available via "AI Assistant" button in top toolbar

#### For "create-connection [provider]"

1. Navigate to connections section
2. Take snapshot to find "Create" or "Add" button
3. Click create button
4. Wait for create dialog/form
5. Take snapshot to see form fields
6. Fill in fields based on provider:
   - **Name**: Auto-generate or use argument
   - **Provider**: Select from dropdown (OpenAI, Anthropic, etc.)
   - **API Key**: Use placeholder or prompt user
7. Click save button
8. Wait for success notification
9. Report: "Connection '[name]' created successfully"

**Playwright pattern:**
```
browser_navigate (to connections)
browser_snapshot (find create button)
browser_click (create button ref)
browser_snapshot (see form)
browser_fill_form (name + provider + api key)
browser_click (save button ref)
browser_wait_for (success message)
```

#### For "edit-connection [name]"

1. Navigate to connections section
2. Take snapshot to find connection by name
3. Click edit button/icon for the connection
4. Wait for edit dialog/form
5. Take snapshot to show editable fields
6. Inform user: "Edit form loaded. What would you like to change?"
7. Wait for user instructions on what to edit
8. Apply changes using browser_fill_form or browser_type
9. Click save
10. Report: "Connection '[name]' updated successfully"

#### For "create-profile [capability]"

Similar pattern to create-connection, but navigate to `/umbraco#/ai/profiles` and use capability (chat/embedding) in form.

#### For "create-prompt"

Navigate to `/umbraco#/ai-prompt/prompts` (requires Umbraco.Ai.Prompt package). Follow create pattern with prompt-specific fields (name, template, variables).

#### For "create-agent"

Navigate to `/umbraco#/ai-agent/agents` (requires Umbraco.Ai.Agent package). Follow create pattern with agent-specific fields (name, profile, tools, system prompt).

#### For "navigate-to-copilot"

1. Navigate to `/umbraco#/ai-agent/copilot`
2. Wait for Copilot UI to load
3. Take snapshot showing chat interface
4. Report: "Copilot chat UI loaded. Ready for interactions."

#### For "status"

1. Check if demo-site is running: `/demo-site status`
2. Try to connect browser to demo site URL
3. Take snapshot if connected
4. Report:
   - Demo site status: running/not running
   - Browser status: connected/not connected
   - Current page: URL and title
   - Suggestion: How to proceed

### Step 4: Error Handling

Common errors and solutions:

| Error | Solution |
|-------|----------|
| Browser not installed | Run `mcp__playwright__browser_install` |
| Demo site not running | Run `/demo-site start` first |
| Login failed | Check credentials, take snapshot for debugging |
| Element not found | Take snapshot, update selectors based on actual HTML |
| Navigation timeout | Increase wait time, check for loading indicators |

## Umbraco Backoffice Structure

### Common Element Patterns

Umbraco backoffice uses Umbraco UI Library (Lit components):

- **Buttons**: `<uui-button>` elements with `label` attribute
- **Inputs**: `<uui-input>` elements with `name` attribute
- **Selects**: `<uui-select>` or custom dropdowns
- **Dialogs**: `<umb-modal-dialog>` elements
- **Tables**: `<uui-table>` with rows/cells
- **Navigation**: `<umb-section-sidebar>` and `<umb-workspace>` elements

### Taking Snapshots for Discovery

Always take a snapshot before interacting to:
1. Verify page state (logged in, correct section, etc.)
2. Find element references (ref attribute) for clicks/fills
3. Understand current UI state
4. Debug issues when interactions fail

Use `browser_snapshot` liberally - it's fast and provides crucial context.

## Integration with demo-site Skill

This skill depends on the `demo-site` skill for infrastructure:

```bash
# Typical workflow
/demo-site start           # Start the demo site
/demo-site-ix login        # Login via browser
/demo-site-ix navigate-to-connections  # Navigate to section
/demo-site-ix create-connection OpenAI # Create entity
```

## Playwright MCP Tools Reference

Key tools for this skill:

| Tool | Purpose | When to Use |
|------|---------|-------------|
| `browser_navigate` | Go to URL | Initial navigation, section changes |
| `browser_snapshot` | Capture page state | Before/after actions, discovery |
| `browser_click` | Click element | Buttons, links, icons |
| `browser_fill_form` | Fill multiple fields | Login, create/edit forms |
| `browser_type` | Type text slowly | Rich text editors, search boxes |
| `browser_wait_for` | Wait for element/text | After navigation, async loading |
| `browser_take_screenshot` | Visual screenshot | Debugging, visual confirmation |
| `browser_evaluate` | Run JavaScript | Complex interactions, data extraction |

## Success Criteria

**After login**: User sees Umbraco dashboard
**After navigation**: Correct section visible with list of entities
**After create**: New entity appears in list with success notification
**After edit**: Changes saved and reflected in list
**After status**: Clear report of demo site and browser state

## Example Usage

```bash
# Start demo site and login
/demo-site start
/demo-site-ix login

# Create a connection
/demo-site-ix create-connection OpenAI

# Navigate and edit
/demo-site-ix navigate-to-profiles
/demo-site-ix edit-profile "Default Chat"

# Work with add-on packages
/demo-site-ix navigate-to-prompts
/demo-site-ix create-prompt

# Check status anytime
/demo-site-ix status
```

## Tips

- Always check if demo site is running before browser interactions
- Take snapshots frequently to understand page state
- Use `browser_snapshot` instead of `browser_take_screenshot` for actions (faster, more accurate)
- Wait 2-3 seconds after navigation for Lit components to render
- If elements not found, take snapshot and adjust selectors
- Use browser_evaluate for complex queries (e.g., counting entities)
- Test with different providers (OpenAI, Anthropic, Amazon, Google)
