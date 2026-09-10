import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UAI_ENTITY_ADAPTER_CONTEXT } from "../../entity-adapter/entity-adapter.context-token.js";
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
	constructor(host: UmbControllerHost) {
		super(host);
	}

	async contribute(context: UaiRequestContext): Promise<void> {
		// Consumed rather than constructed here -- a separate instance would have its own
		// selection state that never hears about the user's actual choice in the context
		// selector, and would fall back to auto-selecting the last-detected entity instead
		// (whichever workspace happened to re-register itself most recently). A rejection or an
		// undefined result from no provider being present (e.g. this surface isn't hosted under
		// Copilot) both mean "nothing to contribute" -- the former propagates to the collector,
		// which already treats a failing contributor as a no-op. See umbraco/Umbraco.AI#353.
		const entityAdapterContext = await this.getContext(UAI_ENTITY_ADAPTER_CONTEXT);
		if (!entityAdapterContext) return;

		const serialized = await entityAdapterContext.serializeSelectedEntity();
		if (!serialized) return;

		context.add(createEntityContextItem(serialized));
	}
}
