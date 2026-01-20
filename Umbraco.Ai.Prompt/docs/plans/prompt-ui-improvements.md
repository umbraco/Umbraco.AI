# Plan: Improve SEO Descriptions Property Action UI

## Prerequisites

**This work should be done in a new worktree** to keep it isolated from other development:

```bash
bash /Users/philw/.claude/plugins/cache/umbraco-cc-plugins/umb-flow/1.1.0/skills/git-worktree/scripts/worktree-manager.sh create feature/prompt-ui-improvements
```

**IMPORTANT: Use umbraco-backoffice-skills BEFORE implementing.** These skills provide up-to-date patterns and examples. Invoke them at the start of implementation:
- `/umbraco-backoffice-skills:umbraco-modals` - **Critical for modal/sidebar implementation** - shows `type: 'sidebar'` option
- `/umbraco-backoffice-skills:umbraco-umbraco-element` - For Lit element patterns
- `/umbraco-backoffice-skills:umbraco-context-api` - For context consumption patterns
- `/umbraco-backoffice-skills:umbraco-property-action` - For property action patterns

---

## Lessons Learned (Post-Implementation)

### 1. Always Use the Umbraco Modal System

**Problem:** Initially attempted to create a custom drawer/panel element by appending to `document.body`, similar to the Copilot sidebar.

**Why it failed:** Elements appended directly to `document.body` are outside the Umbraco backoffice context and don't have access to the icon registry. Icons (`<uui-icon>`) don't render.

**Solution:** Use the Umbraco modal system with `type: 'sidebar'` instead of creating custom drawer elements. The modal system handles:
- Proper context inheritance (icons work)
- Focus management
- Backdrop/overlay
- Animation
- Accessibility

```typescript
// CORRECT: Use sidebar modal type
export const UAI_PROMPT_PREVIEW_SIDEBAR = new UmbModalToken<Data, Value>(
    MODAL_ALIAS,
    {
        modal: {
            type: 'sidebar',  // Slides in from right
            size: 'small',
        },
    }
);

// INCORRECT: Custom drawer appended to body
// Icons won't work, context not available
document.body.appendChild(customDrawerElement);
```

### 2. Invoke Skills Before Implementing

The umbraco-modals skill documentation shows that modals support `type: 'dialog'` (centered) and `type: 'sidebar'` (slide-in from right). This would have avoided the custom drawer approach entirely.

---

## Current State

The SEO Descriptions property action currently uses a centered modal dialog (`prompt-preview-modal.element.ts`) that:
- Fixed width of 500px
- Shows prompt description, loading state, and AI response
- Has Cancel, Copy Response, and Insert Response buttons
- Response area: min 150px, max 300px height

**Screenshot reference:** The current dialog appears small and "weak" - response text is cramped in a small scrollable area.

## Two Options

---

## Option A: Sidebar Modal (Recommended)

Use the Umbraco modal system with `type: 'sidebar'` to create a slide-in panel from the right.

> **Note:** Do NOT create a custom drawer element like the Copilot sidebar. The Copilot sidebar is a persistent global element that's part of the app structure. For temporary UI triggered by property actions, use the modal system.

### Benefits
- More space for response content (full height)
- User can still see the form field being edited
- Proper context inheritance (icons, localization work correctly)
- Focus management and accessibility handled by modal system
- Same element code works for both dialog and sidebar modes

### Implementation

**Files to modify:**
- `prompt-preview-modal.token.ts` - Add sidebar modal token
- `prompt-insert.property-action.ts` - Select token based on uiMode

**Add sidebar token:**
```typescript
// In prompt-preview-modal.token.ts

// Existing dialog token
export const UAI_PROMPT_PREVIEW_MODAL = new UmbModalToken<Data, Value>(
    MODAL_ALIAS,
    {
        modal: {
            type: 'dialog',
            size: 'medium',
        },
    }
);

// NEW: Sidebar token - uses same element, different layout
export const UAI_PROMPT_PREVIEW_SIDEBAR = new UmbModalToken<Data, Value>(
    MODAL_ALIAS,  // Same alias = same element
    {
        modal: {
            type: 'sidebar',  // Slides in from right
            size: 'small',
        },
    }
);
```

**Update property action:**
```typescript
// In prompt-insert.property-action.ts
const uiMode = meta.uiMode ?? 'modal';
const modalToken = uiMode === 'panel'
    ? UAI_PROMPT_PREVIEW_SIDEBAR
    : UAI_PROMPT_PREVIEW_MODAL;

const result = await umbOpenModal(this, modalToken, { data });
```

**Update modal element styles for responsive width:**
```css
#content {
    width: 650px;
    max-width: 100%;  /* Allows sidebar to constrain width */
    box-sizing: border-box;
}
```

**Estimated effort:** Low (30 minutes) - just token configuration

---

## Option B: Enhanced Modal

Keep the modal pattern but improve the visual design to feel more substantial.

### Benefits
- Simpler change (less code modification)
- Familiar modal pattern
- No layout shifts

### Implementation

**Files to modify:**
- `Umbraco.Ai.Prompt/src/Umbraco.Ai.Prompt.Web.StaticAssets/Client/src/prompt/property-actions/prompt-preview-modal.element.ts`

**Improvements:**
1. **Larger size** - Increase width from 500px to 600-700px
2. **Taller response area** - Increase max-height from 300px to 450px
3. **Better typography** - Larger font size, better line height for response
4. **Visual hierarchy** - Add subtle background/card for response area
5. **AI branding** - Add sparkles icon or AI indicator in header
6. **Regenerate button** - Add ability to regenerate response

**Updated styling:**
```css
#content {
  width: 650px;  /* was 500px */
}

.response-container {
  min-height: 200px;  /* was 150px */
  max-height: 450px;  /* was 300px */
  background: var(--uui-color-surface-alt);  /* subtle differentiation */
}

.response-content {
  font-size: var(--uui-type-default-size);  /* was small-size */
  line-height: 1.6;  /* was 1.5 */
}

/* Add header styling */
.response-section h4 {
  display: flex;
  align-items: center;
  gap: var(--uui-size-space-2);
}
.response-section h4::before {
  content: '';
  /* AI sparkles icon */
}
```

**Additional enhancements:**
- Add "Regenerate" button next to response header
- Show character count for SEO (meta descriptions should be ~155-160 chars)
- Add visual indicator if response is too long/short for SEO

**Estimated effort:** Low (1-2 hours)

---

## Implementation Plan: Both Options

We will implement **both options** using the Umbraco modal system - the same modal element can render as either dialog or sidebar.

### Approach

1. **Enhance existing Modal element** (`prompt-preview-modal.element.ts`) with improved styling
2. **Add sidebar modal token** (`prompt-preview-modal.token.ts`) - same element, different layout
3. **Add configuration** - Property action selects token based on `uiMode` in manifest meta

### File Changes

**Modified files:**
- `prompt-preview-modal.element.ts` - Enhanced styling, regenerate button, character indicator
- `prompt-preview-modal.token.ts` - Add `UAI_PROMPT_PREVIEW_SIDEBAR` token
- `prompt-insert.property-action.ts` - Select modal token based on uiMode
- `generate-prompt-property-action-manifest.ts` - Add `uiMode` to manifest meta
- `types.ts` - Add `UaiPromptUiMode` type

**No new element files needed** - the modal system handles both layouts.

### UI Mode Selection

The prompt manifest can specify which UI to use:
```typescript
meta: {
  uiMode: 'panel' | 'modal',  // default: 'modal' for backwards compat
  // ... other meta
}
```

This allows different prompts to use different UIs, or the same prompt to be tested with both.

---

## Verification

After implementation:
1. Run `npm run build` in `Umbraco.Ai.Prompt/src/Umbraco.Ai.Prompt.Web.StaticAssets/Client`
2. Start the demo site
3. Navigate to Content > Home > SEO tab
4. Hover over Meta Description and click the ellipsis
5. Click "SEO Descriptions"
6. Verify:
   - Panel/modal appears with improved styling
   - Loading state displays correctly
   - Response renders with good readability
   - Copy and Insert buttons work
   - Panel/modal closes properly
