import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { TestsService } from "../../../api/sdk.gen.js";
import { UaiTestTypeMapper } from "../../type-mapper.js";
import type { UaiTestDetailModel } from "../../types.js";
import { UAI_TEST_ENTITY_TYPE } from "../../constants.js";
import { UAI_EMPTY_GUID } from "../../../core/index.js";

/**
 * Server data source for Test detail operations.
 */
export class UaiTestDetailServerDataSource implements UmbDetailDataSource<UaiTestDetailModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Creates a scaffold for a new test.
     */
    async createScaffold(preset?: Partial<UaiTestDetailModel>) {
        const scaffold: UaiTestDetailModel = {
            unique: UAI_EMPTY_GUID,
            entityType: UAI_TEST_ENTITY_TYPE,
            alias: "",
            name: "",
            description: null,
            testFeatureId: preset?.testFeatureId ?? "",
            testTargetId: preset?.testTargetId ?? "",
            testCaseJson: "",
            graders: [],
            runCount: 0,
            tags: [],
            dateCreated: null,
            dateModified: null,
            version: 0,
            ...preset,
        };

        return { data: scaffold };
    }

    /**
     * Reads a test by its unique identifier.
     */
    async read(unique: string) {
        const { data, error } = await tryExecute(
            this.#host,
            TestsService.getTestByIdOrAlias({ path: { idOrAlias: unique } }),
        );

        if (error || !data) {
            return { error };
        }

        return { data: UaiTestTypeMapper.toDetailModel(data) };
    }

    /**
     * Creates a new test.
     */
    async create(model: UaiTestDetailModel, _parentUnique: string | null) {
        const requestBody = UaiTestTypeMapper.toCreateRequest(model);

        const { response, error } = await tryExecute(
            this.#host,
            TestsService.createTest({ body: requestBody }),
        );

        if (error) {
            return { error };
        }

        // Extract the ID from the Location header
        const locationHeader = response?.headers?.get("Location") ?? "";
        const unique = locationHeader.split("/").pop() ?? "";

        return {
            data: {
                ...model,
                unique,
            },
        };
    }

    /**
     * Updates an existing test.
     */
    async update(model: UaiTestDetailModel) {
        const requestBody = UaiTestTypeMapper.toUpdateRequest(model);

        const { error } = await tryExecute(
            this.#host,
            TestsService.updateTest({
                path: { idOrAlias: model.unique },
                body: requestBody,
            }),
        );

        if (error) {
            return { error };
        }

        // Re-fetch to get updated version and timestamps
        return this.read(model.unique);
    }

    /**
     * Deletes a test by its unique identifier.
     */
    async delete(unique: string) {
        const { error } = await tryExecute(
            this.#host,
            TestsService.deleteTest({ path: { idOrAlias: unique } }),
        );

        if (error) {
            return { error };
        }

        return {};
    }
}
