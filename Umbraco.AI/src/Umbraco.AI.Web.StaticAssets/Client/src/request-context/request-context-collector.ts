import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { loadManifestApi } from "@umbraco-cms/backoffice/extension-api";
import {
	UAI_REQUEST_CONTEXT_CONTRIBUTOR_EXTENSION_TYPE,
	UaiRequestContext,
	type ManifestUaiRequestContextContributor,
	type UaiRequestContextContributorApi,
} from "./extension-type.js";
import type { UaiRequestContextItem } from "./types.js";

/**
 * Collects request context from all registered contributors.
 *
 * Queries uaiRequestContextContributor extensions and uses
 * `loadManifestApi` to load + instantiate each API (same pattern
 * as UaiFrontendToolManager).
 *
 * API instances are cached for the lifetime of the collector.
 */
export class UaiRequestContextCollector extends UmbControllerBase {
	readonly #apiCache = new Map<string, UaiRequestContextContributorApi>();

	constructor(host: UmbControllerHost) {
		super(host);
	}

	/**
	 * Collect request context items from all resolved contributors.
	 * Creates a mutable UaiRequestContext, passes it through each
	 * contributor (mirroring the backend Contribute pattern), and
	 * returns the accumulated items.
	 *
	 * @returns Aggregated request context items from all contributors.
	 */
	async collect(): Promise<UaiRequestContextItem[]> {
		const manifests = umbExtensionsRegistry.getByType(
			UAI_REQUEST_CONTEXT_CONTRIBUTOR_EXTENSION_TYPE,
		) as ManifestUaiRequestContextContributor[];

		const context = new UaiRequestContext();

		for (const manifest of manifests) {
			try {
				const api = await this.#getOrLoadApi(manifest);
				if (api) {
					await api.contribute(context, manifest.meta);
				}
			} catch (e) {
				console.error(`[UaiRequestContextCollector] Contributor ${manifest.alias} failed:`, e);
			}
		}

		return context.getItems();
	}

	async #getOrLoadApi(
		manifest: ManifestUaiRequestContextContributor,
	): Promise<UaiRequestContextContributorApi | undefined> {
		const cached = this.#apiCache.get(manifest.alias);
		if (cached) return cached;

		if (!manifest.api) return undefined;

		const ApiConstructor = await loadManifestApi<UaiRequestContextContributorApi>(manifest.api);
		if (!ApiConstructor) return undefined;

		const api = new ApiConstructor(this.getHostElement());
		this.#apiCache.set(manifest.alias, api);
		return api;
	}
}
