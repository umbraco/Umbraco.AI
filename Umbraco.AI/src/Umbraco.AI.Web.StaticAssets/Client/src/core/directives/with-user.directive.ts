import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { directive } from "@umbraco-cms/backoffice/external/lit";
import { UaiWithEntityBaseDirective } from "../../core/directives/with-entity-directive-base.directive.js";
import { UmbUserItemModel, UmbUserItemRepository } from "@umbraco-cms/backoffice/user";

export class WithUserDirective extends UaiWithEntityBaseDirective<UmbUserItemModel> {
    async getEntity(host: UmbControllerHost, unique: string) {
        const repository = new UmbUserItemRepository(host);
        return repository.requestItems([unique]).then(({ data: users }) => {
            return users ? users[0] : undefined;
        });
    }
}

export const uaiWithUser = directive(WithUserDirective);
