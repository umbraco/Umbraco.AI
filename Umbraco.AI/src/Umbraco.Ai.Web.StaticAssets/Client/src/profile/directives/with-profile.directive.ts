import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { directive } from "@umbraco-cms/backoffice/external/lit";
import { UaiProfileDetailRepository } from "../repository";
import { UaiWithEntityBaseDirective } from "../../core/directives/with-entity-directive-base.directive.js";
import { UaiProfileDetailModel } from "../types.js";

export class WithProfileDirective extends UaiWithEntityBaseDirective<UaiProfileDetailModel> {
    async getEntity(host: UmbControllerHost, unique: string)
    {
        const repository = new UaiProfileDetailRepository(host);
        return repository.requestByUnique(unique).then(({ data:profile }) => {
            return profile;
        });
    }
}

export const uaiWithProfile = directive(WithProfileDirective);