import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { directive } from "@umbraco-cms/backoffice/external/lit";
import { UaiProviderDetailRepository } from "../repository";
import { UaiWithEntityBaseDirective } from "../../core/directives/with-entity-directive-base.directive.js";
import type { UaiProviderDetailModel } from "../types.ts";

export class WithProviderDirective extends UaiWithEntityBaseDirective<UaiProviderDetailModel> {
    async getEntity(host: UmbControllerHost, unique: string)
    {
        const repository = new UaiProviderDetailRepository(host);
        return repository.requestById(unique).then(({ data:provider }) => {
            return provider;
        });
    }
}

export const uaiWithProvider = directive(WithProviderDirective);