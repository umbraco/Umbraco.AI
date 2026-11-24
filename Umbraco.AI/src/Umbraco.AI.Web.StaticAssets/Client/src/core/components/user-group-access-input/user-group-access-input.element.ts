import { css, customElement, html, nothing, property, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UMB_CURRENT_USER_CONTEXT } from "@umbraco-cms/backoffice/current-user";
import { UmbUserGroupItemRepository } from "@umbraco-cms/backoffice/user-group";
import { pickUserGroups } from "./user-group-picker.js";
import {
    addUserGroupIds,
    canRemoveUserGroup,
    canUseRestrictedItem,
    removeUserGroupId,
} from "./user-group-access.utils.js";

/**
 * Field for choosing which user groups may use an item. An empty list means every backoffice user.
 * Admins pick from all groups; anyone else picks from their own and can only remove their own
 * groups (never the last). A user outside every listed group sees the list read-only.
 *
 * @fires change - Fires when the list changes
 * @public
 */
@customElement("uai-user-group-access-input")
export class UaiUserGroupAccessInputElement extends UmbLitElement {
    /** The selected user group keys. */
    @property({ type: Array, attribute: false })
    value: string[] = [];

    #userGroupRepository = new UmbUserGroupItemRepository(this);

    @state()
    private _isAdmin = false;

    @state()
    private _userGroupIds: string[] = [];

    /** Resolved group names by key. A key resolved to null no longer exists. Unresolved keys are absent. */
    @state()
    private _userGroupNames = new Map<string, string | null>();

    constructor() {
        super();
        this.consumeContext(UMB_CURRENT_USER_CONTEXT, (context) => {
            this.observe(context?.currentUser, (user) => {
                this._isAdmin = user?.isAdmin ?? false;
                this._userGroupIds = user?.userGroupUniques ?? [];
            });
        });
    }

    protected override willUpdate(changed: Map<string, unknown>) {
        if (changed.has("value")) void this.#resolveNames(this.value);
    }

    /** Looks up names for keys not seen yet. A key the item endpoint doesn't return is a deleted group. */
    async #resolveNames(ids: string[]) {
        const missing = ids.filter((id) => !this._userGroupNames.has(id));
        if (missing.length === 0) return;
        const { data, error } = await this.#userGroupRepository.requestItems(missing);
        if (error) return; // rows keep showing the key
        const found = new Map((data ?? []).map((group) => [group.unique.toLowerCase(), group.name]));
        const names = new Map(this._userGroupNames);
        for (const id of missing) names.set(id, found.get(id.toLowerCase()) ?? null);
        this._userGroupNames = names;
    }

    #setValue(ids: string[]) {
        this.value = ids;
        this.dispatchEvent(new UmbChangeEvent());
    }

    async #choose() {
        const picked = await pickUserGroups(this, this.#userGroupRepository, {
            multiple: true,
            restrictToIds: this._isAdmin ? undefined : this._userGroupIds,
        });
        if (picked.length) this.#setValue(addUserGroupIds(this.value, picked));
    }

    #renderRow(id: string, readonly: boolean) {
        const name = this._userGroupNames.get(id);
        const deleted = name === null;
        const canRemove = !readonly && canRemoveUserGroup(id, this.value, this._userGroupIds, this._isAdmin);
        return html`
            <uui-ref-node
                name=${deleted ? this.localize.term("uaiUserGroupAccess_deletedUserGroup") : (name ?? id)}
                detail=${deleted ? this.localize.term("uaiUserGroupAccess_deletedUserGroupDetail") : ""}
                readonly
            >
                <uui-icon slot="icon" name=${deleted ? "icon-alert" : "icon-users"}></uui-icon>
                ${deleted
                    ? html`<uui-tag size="s" slot="tag" color="danger"
                          >${this.localize.term("general_deleted")}</uui-tag
                      >`
                    : nothing}
                ${canRemove
                    ? html`<uui-action-bar slot="actions">
                          <uui-button
                              label=${this.localize.term("general_remove")}
                              @click=${() => this.#setValue(removeUserGroupId(this.value, id))}
                          >
                              ${this.localize.term("general_remove")}
                          </uui-button>
                      </uui-action-bar>`
                    : nothing}
            </uui-ref-node>
        `;
    }

    override render() {
        const readonly = !canUseRestrictedItem(this.value, this._userGroupIds, this._isAdmin);
        return html`
            <umb-property-layout
                label=${this.localize.term("user_usergroups")}
                description=${this.localize.term(
                    readonly ? "uaiUserGroupAccess_readonlyDescription" : "uaiUserGroupAccess_description",
                )}
            >
                <div slot="editor">
                    <uui-ref-list>
                        ${repeat(
                            this.value,
                            (id) => id,
                            (id) => this.#renderRow(id, readonly),
                        )}
                    </uui-ref-list>
                    ${readonly
                        ? nothing
                        : html`<uui-button
                              id="choose"
                              look="placeholder"
                              label=${this.localize.term("general_choose")}
                              @click=${this.#choose}
                          ></uui-button>`}
                </div>
            </umb-property-layout>
        `;
    }

    static override styles = [
        css`
            :host {
                display: block;
            }

            #choose {
                width: 100%;
            }

            uui-ref-list + #choose {
                margin-top: var(--uui-size-space-2);
            }
        `,
    ];
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-user-group-access-input": UaiUserGroupAccessInputElement;
    }
}
