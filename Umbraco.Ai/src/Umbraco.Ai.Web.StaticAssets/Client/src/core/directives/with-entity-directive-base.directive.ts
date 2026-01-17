import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { AsyncDirective, noChange } from "@umbraco-cms/backoffice/external/lit";

export abstract class UaiWithEntityBaseDirective<T> extends AsyncDirective {

    #template?: (entity:T) => unknown;

    abstract getEntity(host:UmbControllerHost, unique:string): Promise<T | undefined>;

    formatValue = (entity:T) => this.#template
        ? this.#template(entity)
        : undefined;

    render(host:UmbControllerHost, unique:string, template:(currency:T) => unknown) {

        this.#template = template;

        this.getEntity(host, unique).then((entity) => {
            if (entity) {
                this.setValue(this.formatValue(entity));
            }
        });
        
        return noChange;
    }
}
