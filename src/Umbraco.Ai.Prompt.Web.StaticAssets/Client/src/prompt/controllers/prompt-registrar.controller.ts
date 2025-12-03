import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UaiPromptDetailRepository } from "../repository";

export class UmbPromptRegistrarController extends UmbControllerBase {

    // @ts-expect-error - Repository will be used when registerPrompts is implemented
    #repository = new UaiPromptDetailRepository(this);

    registerPrompts() {
        // TODO: Implement prompt registration
    }
}