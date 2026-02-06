# Umbraco Backoffice UI Reference

Reference guide for Umbraco backoffice UI patterns and navigation URLs.

## Contents

- [Section URLs](#section-urls)
- [Element Patterns](#element-patterns)
- [Taking Snapshots](#taking-snapshots)

## Section URLs

All AI sections are under the Settings area:

| Section     | URL Path                                                  |
| ----------- | --------------------------------------------------------- |
| Connections | `/umbraco/section/settings/workspace/uai:connection-root` |
| Profiles    | `/umbraco/section/settings/workspace/uai:profile-root`    |
| Prompts     | `/umbraco/section/settings/workspace/uai:prompt-root`     |
| Agents      | `/umbraco/section/settings/workspace/uai:agent-root`      |
| Contexts    | `/umbraco/section/settings/workspace/uai:context-root`    |
| Analytics   | `/umbraco/section/settings/workspace/ai-analytics-root`   |
| Logs        | `/umbraco/section/settings/workspace/uai:trace-root`      |
| AI Settings | `/umbraco/section/settings/workspace/uai:settings-root`   |
| Copilot     | Via "AI Assistant" button in top toolbar                  |

## Element Patterns

Umbraco backoffice uses Umbraco UI Library (Lit components):

| Element Type | HTML Tag                | Key Attribute      |
| ------------ | ----------------------- | ------------------ |
| Buttons      | `<uui-button>`          | `label`            |
| Inputs       | `<uui-input>`           | `name`             |
| Selects      | `<uui-select>`          | Custom dropdowns   |
| Dialogs      | `<umb-modal-dialog>`    | Modal containers   |
| Tables       | `<uui-table>`           | With rows/cells    |
| Navigation   | `<umb-section-sidebar>` | Sidebar navigation |
| Workspace    | `<umb-workspace>`       | Main content area  |

### Common Interaction Patterns

**Finding buttons:**

```
Look for <uui-button label="Create"> or similar
Use the ref attribute from browser_snapshot for clicks
```

**Finding form fields:**

```
Look for <uui-input name="fieldName"> elements
Use browser_fill_form with field refs
```

**Finding list items:**

```
Look for <uui-table-row> elements within <uui-table>
Each row typically has action buttons (edit, delete)
```

## Taking Snapshots

Always take a snapshot before interacting to:

1. **Verify page state** - Confirm logged in and on correct section
2. **Find element references** - Get `ref` attributes for clicks/fills
3. **Understand UI state** - See what's visible and interactive
4. **Debug failures** - Capture state when interactions fail

Use `browser_snapshot` liberally - it's fast and provides crucial context for all subsequent actions.

### Snapshot Tips

- Take snapshot after every navigation
- Take snapshot before any click or form fill
- Take snapshot after actions to confirm success
- If element not found, snapshot shows actual page structure
