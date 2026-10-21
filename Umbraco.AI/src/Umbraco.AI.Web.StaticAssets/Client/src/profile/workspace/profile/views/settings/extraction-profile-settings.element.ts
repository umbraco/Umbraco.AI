import { css, html, customElement, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiExtractionProfileSettings } from "../../../../types.js";

/**
 * Extraction profile settings.
 *
 * Extraction has no configurable settings today, so this renders a "no settings" message rather than a
 * blank area. It still accepts `settings`/`metadata` properties, matching the shape of its sibling
 * settings elements, so the workspace view can render every capability's settings element uniformly
 * without special-casing this one.
 */
@customElement("uai-extraction-profile-settings")
export class UaiExtractionProfileSettingsElement extends UmbLitElement {
    @property({ type: Object })
    settings: UaiExtractionProfileSettings | null = null;

    /** The selected model descriptor's metadata, carrying the provider's per-model declarations. */
    @property({ type: Object })
    metadata?: Record<string, string>;

    override render() {
        return html`
            <uui-box headline="System Settings">
                <p class="empty">${this.localize.term("uaiProfile_noSettingsAvailable")}</p>
            </uui-box>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            uui-box {
                --uui-box-default-padding: 0 var(--uui-size-space-5);
            }

            .empty {
                color: var(--uui-color-text-alt);
            }
        `,
    ];
}

export default UaiExtractionProfileSettingsElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-extraction-profile-settings": UaiExtractionProfileSettingsElement;
    }
}
