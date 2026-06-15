/**
 * Property Value Preparer
 *
 * Editor-specific frontend adjustment applied to a property value on its way from an AI tool
 * (or any caller of `UaiEntityAdapterContext.applyValueChange`) into `workspaceContext.setPropertyValue`.
 *
 * Preparers exist because some property editor lit components have frontend-only quirks that the
 * backend dispatcher can't fix:
 * - block-shaped editors return pre-built object envelopes that must NOT be re-stringified;
 * - rich-text expects `{ markup, blocks }` and may receive a bare markup string;
 * - media picker 3's thumbnail subcomponent doesn't react to in-place `mediaKey` changes, so we
 *   re-mint the entry's `key` to force lit to re-mount the subcomponent.
 *
 * Each preparer owns ALL the editor-specific behavior — including any JSON.parse on string input,
 * any shape adjustments, and any reactivity workarounds. Preparers are stateless and pure.
 *
 * Third parties register a preparer for their own editor's quirks via
 * `uaiPropertyValuePreparer` extension manifest. Editors with no registered preparer fall back to
 * a default that attempts JSON.parse on string inputs and returns the result.
 */
export interface UaiPropertyValuePreparerApi {
    /**
     * Adjust a value on its way to `setPropertyValue`.
     *
     * @param value The new value being applied (whatever shape the caller produced).
     * @param currentValue The value currently staged in the workspace, if any. Used by preparers
     * that need to compare incoming entries to existing ones (e.g. to detect content changes).
     * @returns The adjusted value to pass through to `setPropertyValue`.
     */
    prepare(value: unknown, currentValue: unknown): unknown | Promise<unknown>;
}
