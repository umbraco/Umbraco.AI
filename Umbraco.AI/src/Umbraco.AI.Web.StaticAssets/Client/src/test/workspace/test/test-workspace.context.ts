import type { UmbRoutableWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import {
    UmbWorkspaceRouteManager,
    UmbSubmittableWorkspaceContextBase,
    UmbWorkspaceIsNewRedirectController,
    UmbWorkspaceIsNewRedirectControllerAlias,
} from "@umbraco-cms/backoffice/workspace";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbBasicState, UmbObjectState } from "@umbraco-cms/backoffice/observable-api";
import { UmbEntityContext } from "@umbraco-cms/backoffice/entity";
import { UAI_TEST_WORKSPACE_ALIAS, UAI_TEST_ENTITY_TYPE } from "../../constants.js";
import { UAI_TEST_ROOT_WORKSPACE_PATH } from "../test-root/paths.js";
import { AITestRepository } from "../../repository/test.repository.js";
import { UmbracoAITestWorkspaceEditorElement } from "./test-workspace-editor.element.js";
import type {
    TestResponseModel,
    CreateTestRequestModel,
    UpdateTestRequestModel,
} from "../../../api/types.gen.js";

/**
 * Workspace context for editing Test entities.
 * Handles CRUD operations and state management with correct model structure.
 */
export class UaiTestWorkspaceContext
    extends UmbSubmittableWorkspaceContextBase<TestResponseModel>
    implements UmbRoutableWorkspaceContext
{
    readonly routes = new UmbWorkspaceRouteManager(this);

    #unique = new UmbBasicState<string | undefined>(undefined);
    readonly unique = this.#unique.asObservable();

    #model = new UmbObjectState<TestResponseModel | undefined>(undefined);
    readonly model = this.#model.asObservable();

    #repository: AITestRepository;
    #entityContext = new UmbEntityContext(this);

    constructor(host: UmbControllerHost) {
        super(host, UAI_TEST_WORKSPACE_ALIAS);

        this.#repository = new AITestRepository(this);
        this.#entityContext.setEntityType(UAI_TEST_ENTITY_TYPE);
        this.observe(this.unique, (unique) => this.#entityContext.setUnique(unique ?? null));

        this.routes.setRoutes([
            {
                path: "create/:testFeatureId",
                component: UmbracoAITestWorkspaceEditorElement,
                setup: (_component, info) => {
                    const testFeatureId = info.match.params.testFeatureId;
                    this.#createScaffold(testFeatureId);
                    new UmbWorkspaceIsNewRedirectController(
                        this,
                        this,
                        this.getHostElement().shadowRoot!.querySelector("umb-router-slot")!
                    );
                },
            },
            {
                path: "edit/:id",
                component: UmbracoAITestWorkspaceEditorElement,
                setup: (_component, info) => {
                    this.removeUmbControllerByAlias(UmbWorkspaceIsNewRedirectControllerAlias);
                    const id = info.match.params.id;
                    this.load(id);
                },
            },
        ]);
    }

    protected resetState(): void {
        super.resetState();
        this.#unique.setValue(undefined);
        this.#model.setValue(undefined);
    }

    async load(unique: string) {
        const data = await this.#repository.getTestByIdOrAlias(unique);
        if (data) {
            this.#unique.setValue(data.id);
            this.#model.setValue(data);
            this.setIsNew(false);
        }
    }

    #createScaffold(testFeatureId: string) {
        this.resetState();
        const scaffold: TestResponseModel = {
            id: crypto.randomUUID(),
            alias: "",
            name: "",
            description: null,
            testFeatureId: testFeatureId,
            target: {
                targetId: "",
                isAlias: false,
            },
            testCaseJson: "{}",
            graders: [],
            runCount: 3,
            tags: [],
            dateCreated: new Date().toISOString(),
            dateModified: new Date().toISOString(),
            version: 1,
        };

        this.#unique.setValue(scaffold.id);
        this.#model.setValue(scaffold);
        this.setIsNew(true);
    }

    getData() {
        return this.#model.getValue();
    }

    getUnique() {
        return this.#unique.getValue();
    }

    getEntityType() {
        return UAI_TEST_ENTITY_TYPE;
    }

    async submit() {
        const model = this.getData();
        if (!model) {
            throw new Error("No model to submit");
        }

        if (this.getIsNew()) {
            const request: CreateTestRequestModel = {
                alias: model.alias,
                name: model.name,
                description: model.description || undefined,
                testFeatureId: model.testFeatureId,
                target: model.target,
                testCaseJson: model.testCaseJson,
                graders: model.graders,
                runCount: model.runCount,
                tags: model.tags,
            };

            const id = await this.#repository.createTest(request);
            this.#unique.setValue(id);
            this.setIsNew(false);
        } else {
            const request: UpdateTestRequestModel = {
                alias: model.alias,
                name: model.name,
                description: model.description || undefined,
                testFeatureId: model.testFeatureId,
                target: model.target,
                testCaseJson: model.testCaseJson,
                graders: model.graders,
                runCount: model.runCount,
                tags: model.tags,
            };

            await this.#repository.updateTest(model.id, request);
        }
    }

    async delete() {
        const id = this.getUnique();
        if (id) {
            await this.#repository.deleteTest(id);
            window.location.hash = UAI_TEST_ROOT_WORKSPACE_PATH;
        }
    }

    public destroy(): void {
        this.#model.destroy();
        this.#unique.destroy();
        super.destroy();
    }
}

export default UaiTestWorkspaceContext;
