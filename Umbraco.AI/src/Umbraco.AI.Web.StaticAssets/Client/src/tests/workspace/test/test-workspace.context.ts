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
import { UAI_TEST_ROOT_WORKSPACE_PATH } from "../tests-root/paths.js";
import { UaiTestWorkspaceEditorElement } from "./test-workspace-editor.element.js";
import { AITestRepository } from "../../repository/test.repository.js";
import type { TestResponseModel, CreateTestRequestModel, UpdateTestRequestModel } from "../../../api/client/index.js";

/**
 * Workspace context for editing Test entities.
 * Handles CRUD operations and state management.
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
				path: "create/:testType",
				component: UaiTestWorkspaceEditorElement,
				setup: async (_component, info) => {
					const testType = info.match.params.testType;
					await this.scaffold(testType);
					new UmbWorkspaceIsNewRedirectController(
						this,
						this,
						this.getHostElement().shadowRoot!.querySelector("umb-router-slot")!,
					);
				},
			},
			{
				path: "edit/:unique",
				component: UaiTestWorkspaceEditorElement,
				setup: (_component, info) => {
					this.removeUmbControllerByAlias(UmbWorkspaceIsNewRedirectControllerAlias);
					this.load(info.match.params.unique);
				},
			},
		]);
	}

	protected resetState(): void {
		super.resetState();
		this.#unique.setValue(undefined);
		this.#model.setValue(undefined);
	}

	/**
	 * Creates a scaffold for a new test.
	 */
	async scaffold(testType?: string) {
		this.resetState();

		const data: TestResponseModel = {
			id: "",
			alias: "",
			name: "",
			description: "",
			testTypeId: testType || "prompt",
			target: {
				profileIdOrAlias: "",
				contextIds: [],
			},
			testCase: {},
			graders: [],
			runCount: 1,
			tags: [],
		};

		this.setIsNew(true);
		this.#model.setValue(data);
		return { data };
	}

	/**
	 * Loads an existing test by ID or alias.
	 */
	async load(unique: string) {
		this.resetState();

		const test = await this.#repository.getById(unique);
		if (!test) {
			throw new Error(`Test with ID ${unique} not found`);
		}

		this.setIsNew(false);
		this.#unique.setValue(test.id);
		this.#model.setValue(test);
	}

	/**
	 * Gets the current test data.
	 */
	getData() {
		return this.#model.getValue();
	}

	/**
	 * Gets the unique identifier of the current test.
	 */
	getUnique() {
		return this.#unique.getValue();
	}

	/**
	 * Gets the entity type.
	 */
	getEntityType() {
		return UAI_TEST_ENTITY_TYPE;
	}

	/**
	 * Creates or updates the test.
	 */
	async submit() {
		const model = this.getData();
		if (!model) {
			throw new Error("No test data to save");
		}

		if (this.getIsNew()) {
			// Create new test
			const createModel: CreateTestRequestModel = {
				alias: model.alias,
				name: model.name,
				description: model.description,
				testTypeId: model.testTypeId,
				target: model.target,
				testCase: model.testCase,
				graders: model.graders,
				runCount: model.runCount,
				tags: model.tags,
			};

			const id = await this.#repository.create(createModel);
			this.#unique.setValue(id);

			// Reload the created test
			await this.load(id);

			// Navigate to edit mode
			const path = `/section/umbraco-ai/workspace/uai:test/edit/${id}`;
			history.pushState(null, "", path);
		} else {
			// Update existing test
			const updateModel: UpdateTestRequestModel = {
				name: model.name,
				description: model.description,
				target: model.target,
				testCase: model.testCase,
				graders: model.graders,
				runCount: model.runCount,
				tags: model.tags,
			};

			const unique = this.getUnique();
			if (!unique) throw new Error("No unique ID for test");

			await this.#repository.update(unique, updateModel);

			// Reload the updated test
			await this.load(unique);
		}
	}

	/**
	 * Deletes the current test.
	 */
	async delete() {
		const unique = this.getUnique();
		if (!unique) throw new Error("No test to delete");

		await this.#repository.delete(unique);

		// Navigate back to list
		history.pushState(null, "", UAI_TEST_ROOT_WORKSPACE_PATH);
	}
}

export { UaiTestWorkspaceContext as api };
export default UaiTestWorkspaceContext;
