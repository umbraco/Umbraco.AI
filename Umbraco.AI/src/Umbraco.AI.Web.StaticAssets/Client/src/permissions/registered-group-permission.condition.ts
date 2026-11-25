import { UMB_CURRENT_USER_CONTEXT } from "@umbraco-cms/backoffice/current-user";
import type { UmbConditionConfigBase, UmbConditionControllerArguments } from "@umbraco-cms/backoffice/extension-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbConditionBase, umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { UAI_PERMISSION_ENTITY_TYPE, UAI_REGISTERED_GROUP_PERMISSION_CONDITION_ALIAS } from "./constants.js";

export type UaiRegisteredGroupPermissionConditionConfig = UmbConditionConfigBase<
    typeof UAI_REGISTERED_GROUP_PERMISSION_CONDITION_ALIAS
> & {
    /** The `meta.group` of the rights to look for among the registered AI rights. */
    group: string;
};

type RegisteredPermission = { meta?: { group?: string; verbs?: Array<string> } };

/**
 * Permitted when the current user holds any right registered in a group. The rights come from the
 * extension registry, so an add-on's right joins the group just by registering, and core keeps no
 * list of add-on rights. Not permitted while the registry has no right in the group.
 */
export class UaiRegisteredGroupPermissionCondition extends UmbConditionBase<UaiRegisteredGroupPermissionConditionConfig> {
    #verbs: Array<string> = [];
    #userPermissions: Array<string> = [];

    constructor(
        host: UmbControllerHost,
        args: UmbConditionControllerArguments<UaiRegisteredGroupPermissionConditionConfig>,
    ) {
        super(host, args);

        this.observe(
            umbExtensionsRegistry.byTypeAndFilter(
                "entityUserPermission",
                (manifest) =>
                    (manifest.forEntityTypes ?? []).includes(UAI_PERMISSION_ENTITY_TYPE) &&
                    (manifest as RegisteredPermission).meta?.group === this.config.group,
            ),
            (manifests) => {
                this.#verbs = manifests.flatMap((manifest) => (manifest as RegisteredPermission).meta?.verbs ?? []);
                this.#check();
            },
            "uaiRegisteredGroupPermissionRegistryObserver",
        );

        this.consumeContext(UMB_CURRENT_USER_CONTEXT, (context) => {
            this.observe(
                context?.fallbackPermissions,
                (permissions) => {
                    this.#userPermissions = permissions ?? [];
                    this.#check();
                },
                "uaiRegisteredGroupPermissionUserObserver",
            );
        });
    }

    #check() {
        this.permitted = this.#verbs.some((verb) => this.#userPermissions.includes(verb));
    }
}

export { UaiRegisteredGroupPermissionCondition as api };
