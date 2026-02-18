import type { UaiRequestContextContributorApi } from "../extension-type.js";
import type { UaiRequestContext } from "../extension-type.js";

/**
 * Contributes the current backoffice section to the request context.
 * Frontend counterpart of backend SectionContextContributor.
 *
 * Unconditional -- always contributes when a section is detected.
 */
export default class UaiSectionRequestContextContributor implements UaiRequestContextContributorApi {
	async contribute(context: UaiRequestContext): Promise<void> {
		const section = this.#getSectionFromUrl();
		if (!section) return;

		context.add({
			description: `Current section: ${section}`,
			value: JSON.stringify({ section }),
		});
	}

	#getSectionFromUrl(): string | null {
		const match = window.location.pathname.match(/\/section\/([^/]+)/);
		return match?.[1] ?? null;
	}

	destroy(): void {
		/* no-op */
	}
}
