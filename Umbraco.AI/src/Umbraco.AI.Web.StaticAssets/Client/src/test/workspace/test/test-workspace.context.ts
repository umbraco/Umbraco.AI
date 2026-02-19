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
import { UmbValidationContext } from "@umbraco-cms/backoffice/validation";
import { UAI_TEST_WORKSPACE_ALIAS, UAI_TEST_ENTITY_TYPE } from "../../constants.js";
import { UAI_TEST_ROOT_WORKSPACE_PATH } from "../test-root/paths.js";
import { AITestRepository } from "../../repository/test.repository.js";
import { UmbracoAITestWorkspaceEditorElement } from "./views/test-workspace-editor.element.js";
import type { UaiCommand } from "../../../core/command/command.base.js";
import { UaiCommandStore } from "../../../core/command/command.store.js";
import { UaiEntityDeletedRedirectController } from "../../../core/workspace/entity-deleted-redirect.controller.js";
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
    #commandStore = new UaiCommandStore();
    #entityContext = new UmbEntityContext(this);
    #validationContext = new UmbValidationContext(this);

    // Expose validation context publicly so editor elements can register validators
    get validation() {
        return this.#validationContext;
    }

    constructor(host: UmbControllerHost) {
        super(host, UAI_TEST_WORKSPACE_ALIAS);

        this.#repository = new AITestRepository(this);
        this.addValidationContext(this.#validationContext);

        this.#entityContext.setEntityType(UAI_TEST_ENTITY_TYPE);
        this.observe(this.unique, (unique) => this.#entityContext.setUnique(unique ?? null));

        this.routes.setRoutes([
            {
                path: "create/:testFeatureId",
                component: UmbracoAITestWorkspaceEditorElement,
                setup: async (_component, info) => {
                    const testFeatureId = info.match.params.testFeatureId;
                    await this.scaffold(testFeatureId);
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

                    // Redirect to collection view when entity is deleted
                    new UaiEntityDeletedRedirectController(this, {
                        getUnique: () => this.getUnique(),
                        getEntityType: () => this.getEntityType(),
                        collectionPath: UAI_TEST_ROOT_WORKSPACE_PATH,
                    });
                },
            },
        ]);
    }

    protected resetState(): void {
        super.resetState();
        this.#unique.setValue(undefined);
        this.#model.setValue(undefined);
        this.#commandStore.reset();
    }

    /**
     * Loads an existing test by ID or alias.
     */
    async load(unique: string) {
        const data = await this.#repository.getTestByIdOrAlias(unique);
        if (data) {
            this.#unique.setValue(data.id);
            this.#model.setValue(data);
            this.setIsNew(false);
        }
    }

    /**
     * Reloads the current test.
     */
    async reload() {
        const unique = this.getUnique();
        if (unique) {
            await this.load(unique);
        }
    }

    /**
     * Creates a scaffold for a new test.
     */
    async scaffold(testFeatureId?: string) {
        this.resetState();
        const scaffold: TestResponseModel = {
            id: crypto.randomUUID(),
            alias: "",
            name: "",
            description: null,
            testFeatureId: testFeatureId || "",
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

    /**
     * Handles a command to update the model.
     * Commands are tracked for replay after model refresh.
     */
    handleCommand(command: UaiCommand) {
        const currentValue = this.#model.getValue();
        if (currentValue) {
            const newValue = structuredClone(currentValue);
            command.execute(newValue);
            this.#model.setValue(newValue);
            this.#commandStore.add(command);
        }
    }

    getData(): TestResponseModel | undefined {
        return this.#model.getValue();
    }

    getUnique(): string | undefined {
        return this.#unique.getValue();
    }

    getEntityType(): string {
        return UAI_TEST_ENTITY_TYPE;
    }

    /**
     * Saves the test (create or update).
     */
    async submit() {
        const model = this.getData();
        if (!model) {
            throw new Error("No model to submit");
        }

        // Validate before submit
        try {
            await this.#validationContext.validate();
        } catch {
            // Validation failed - focus first invalid element
            this.#validationContext.focusFirstInvalidElement();
            return;
        }

        // Mute command store during submit
        this.#commandStore.mute();

        try {
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
                    target: model.target,
                    testCaseJson: model.testCaseJson,
                    graders: model.graders,
                    runCount: model.runCount,
                    tags: model.tags,
                };

                await this.#repository.updateTest(model.id, request);
            }

            this.#commandStore.reset();
        } finally {
            this.#commandStore.unmute();
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

export { UaiTestWorkspaceContext as api };
