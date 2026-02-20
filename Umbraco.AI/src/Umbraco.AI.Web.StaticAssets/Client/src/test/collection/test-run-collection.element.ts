import { customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbCollectionDefaultElement } from "@umbraco-cms/backoffice/collection";

/**
 * Collection element for Test Runs.
 */
@customElement("uai-test-run-collection")
export class UaiTestRunCollectionElement extends UmbCollectionDefaultElement {}

export { UaiTestRunCollectionElement as element };

declare global {
    interface HTMLElementTagNameMap {
        "uai-test-run-collection": UaiTestRunCollectionElement;
    }
}
