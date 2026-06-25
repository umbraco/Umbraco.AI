import { UmbCollectionActionBase } from "@umbraco-cms/backoffice/collection";

/**
 * No-op `collectionAction` api.
 *
 * v18's `collectionAction` extension type is registered via `umb-extension-with-api-slot`, which only
 * instantiates the element once *both* an element and an api are present (CMS PR #21974). When a
 * collection action's behaviour is entirely driven by its custom element (popovers, navigation,
 * dropdowns), there's nothing meaningful for the framework's `execute()` callback to do — but the
 * api still has to be there for the slot to render. This class is that placeholder.
 *
 * Pair it with a custom `element` in the manifest:
 *
 * ```ts
 * {
 *     type: "collectionAction",
 *     alias: "...",
 *     element: () => import("./my-create-action.element.js"),
 *     api: UaiNoOpCollectionAction,
 *     meta: { label: "Create ..." },
 * }
 * ```
 */
export class UaiNoOpCollectionAction extends UmbCollectionActionBase {
    public async execute(): Promise<void> {
        // No-op — the element renders its own UI and handles its own clicks/navigation.
    }
}
