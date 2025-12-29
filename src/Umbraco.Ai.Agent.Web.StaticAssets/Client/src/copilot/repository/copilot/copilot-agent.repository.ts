import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import {  UaiCopilotAgentServerDataSource } from "./copilot-agent.server.data-source.js";
import { UaiCopilotAgentStore } from "./copilot-agent.store.js";
import { UaiCopilotAgentItem } from "../../types.js";

/**
 * Repository for loading active agents for the copilot.
 */
export class UaiCopilotAgentRepository extends UmbRepositoryBase {

    #dataSource: UaiCopilotAgentServerDataSource;
    #store?: UaiCopilotAgentStore;
    
    private _agents: UaiCopilotAgentItem[] = [];
    
    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiCopilotAgentServerDataSource(host);
        this.#store = new UaiCopilotAgentStore(host);
        this.#store.all().subscribe(items => {
            this._agents = items;
        });
    }

    async #ensurePopulatedStore() {
        if (this._agents.length === 0) {
            const { error } = await this.fetchAll();
            if (error) return { error }
        }
        return { error: undefined };
    }

    async fetchAll() {
        
        const { data, error } = await this.#dataSource.requestActiveAgents().then(res => {
            if (res.data) {
                this.#store?.appendItems(res.data);
                // Auto select first agent when none is selected.
                if (!this.#store?.getSelected() && res.data.length > 0) {
                    this.#store?.setSelected(res.data[0]);
                }
            }
            return res;
        });

        return { data, error, asObservable: () => this.#store!.all() };
    }
    
    async getOrFetchAll() {

        const r = await this.#ensurePopulatedStore();
        if (r.error) return { error: r.error };

        return { data: this._agents, asObservable: () => this.#store!.all() };
    }

    async fetch(id: string) {
        return this.getOrFetch(id);
    }
    
    async getOrFetch(id: string) {

        const r = await this.#ensurePopulatedStore();
        if (r.error) return { error: r.error };

        const predicateFunc = (x:UaiCopilotAgentItem) => x.id === id;
        return { data: this._agents.find(predicateFunc), asObservable: () => this.#store!.all() };
    }
    
    async select(id: string | undefined) {
        const r = await this.#ensurePopulatedStore();
        if (r.error) return { error: r.error };
        
        if (!id) {
            this.#store?.setSelected(undefined);
            return;
        }
        
        const agent = this._agents.find((a) => a.id === id);
        if (agent) {
            this.#store?.setSelected(agent);
        }
    }
    
    async selected() {
        const r = await this.#ensurePopulatedStore();
        if (r.error) return { error: r.error };

        return { data: this.#store?.getSelected(), asObservable: () => this.#store!.selected };
    }
}
