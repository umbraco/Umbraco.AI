import type { UaiRequestContextContributorApi, UaiRequestContext } from "@umbraco-ai/core";

/**
 * Contributes the copilot surface identifier to the request context.
 * Frontend counterpart of backend SurfaceContextContributor.
 *
 * Copilot-scoped -- registered only in the copilot bundle.
 */
export default class UaiCopilotSurfaceRequestContextContributor implements UaiRequestContextContributorApi {
	async contribute(context: UaiRequestContext): Promise<void> {
		context.add({
			description: "surface",
			value: JSON.stringify({ surface: "copilot" }),
		});
	}

	destroy(): void {
		/* no-op */
	}
}
