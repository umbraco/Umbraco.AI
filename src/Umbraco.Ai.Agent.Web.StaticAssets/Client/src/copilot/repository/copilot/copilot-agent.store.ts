import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import { UmbStoreBase } from "@umbraco-cms/backoffice/store";
import { UmbArrayState, UmbBasicState } from "@umbraco-cms/backoffice/observable-api";
import { UaiCopilotAgentItem } from "../../types.js";

export const UAI_COPILOT_AGENT__STORE_CONTEXT = new UmbContextToken<UaiCopilotAgentStore>(
    "UaiCopilotAgentStore"
);

export class UaiCopilotAgentStore extends UmbStoreBase<UaiCopilotAgentItem> {

  protected _selected: UmbBasicState<UaiCopilotAgentItem | undefined> = new UmbBasicState<UaiCopilotAgentItem | undefined>(undefined);
  #selected$ = this._selected.asObservable();

  constructor(host: UmbControllerHost) {
    super(host, UAI_COPILOT_AGENT__STORE_CONTEXT.toString(), new UmbArrayState<UaiCopilotAgentItem>([], (x) => x.id));
  }

  setSelected(item: UaiCopilotAgentItem | undefined): void {
    this._selected.setValue(item)
  }
  
  getSelected(): UaiCopilotAgentItem | undefined {
    return this._selected.getValue();
  }
  
  selected = this.#selected$;
}

export { UaiCopilotAgentStore as api };