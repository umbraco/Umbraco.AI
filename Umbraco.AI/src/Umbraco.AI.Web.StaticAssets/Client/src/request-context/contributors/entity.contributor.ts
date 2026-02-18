import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiEntityAdapterContext } from "../../entity-adapter/entity-adapter.context.js";
import { createEntityContextItem } from "../helpers.js";
import type { UaiRequestContextContributorApi, UaiRequestContext } from "../extension-type.js";

/**
 * Contributes the currently selected entity to the request context.
 * Frontend counterpart of backend SerializedEntityContributor.
 *
 * Unconditional -- always contributes when an entity is selected.
 * No-op if no entity is open or no adapter matches.
 */
export default class UaiEntityRequestContextContributor
	extends UmbControllerBase
	implements UaiRequestContextContributorApi
{
	#entityAdapterContext: UaiEntityAdapterContext;

	constructor(host: UmbControllerHost) {
		super(host);
		this.#entityAdapterContext = new UaiEntityAdapterContext(host);
	}

	async contribute(context: UaiRequestContext): Promise<void> {
		const serialized = await this.#entityAdapterContext.serializeSelectedEntity();
		if (!serialized) return;

		context.add(createEntityContextItem(serialized));
	}
}
