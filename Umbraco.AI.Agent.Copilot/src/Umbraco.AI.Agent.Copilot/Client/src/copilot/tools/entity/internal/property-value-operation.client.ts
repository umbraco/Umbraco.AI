import { tryExecute } from "@umbraco-cms/backoffice/resources";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import {
    PropertyValueOperationsService,
    type AiPropertyOperationModel,
    type AiPropertyValueOperationErrorModel,
    type PropertyValueOperationRequestModel,
    type PropertyValueOperationResponseModel,
} from "@umbraco-ai/core";

/**
 * Path segment shape: alternating property aliases (strings) at even indices and block
 * selectors ({ blockKey: "<guid>" }) at odd indices. The OpenAPI generator can't model the
 * polymorphic shape directly, so we type it locally here and cast when calling the typed client.
 */
export type PropertyPathSegment = string | { blockKey: string };

export type PropertyOperation = AiPropertyOperationModel;
export type PropertyValueOperationError = AiPropertyValueOperationErrorModel;
export type PropertyValueOperationResponse = PropertyValueOperationResponseModel;

/**
 * Variant identifier shape (culture + segment).
 */
export interface VariantId {
    culture: string | null;
    segment: string | null;
}

/**
 * Document-level metadata required by the dispatcher.
 */
export interface DocumentMetadata {
    contentTypeKey: string;
    variants: VariantId[];
    isVariant: boolean;
    isSegmented: boolean;
    name?: string;
}

/**
 * Operation request payload, typed locally so callers don't have to deal with the generator's
 * placeholder for `AiPropertyPathSegmentModel`.
 */
export interface PropertyValueOperationRequest {
    path: PropertyPathSegment[];
    operation: PropertyOperation;
    args?: unknown;
    rootValue?: unknown;
    documentMetadata: DocumentMetadata;
}

/**
 * Invokes the property value operation endpoint via the generated hey-api client.
 *
 * Uses `tryExecute` so the backoffice's bearer-token auth, 401 retry interceptor, and standard
 * error envelope all flow through unchanged. The endpoint is stateless: it never reads from or
 * writes to the database. The caller supplies the staged `rootValue` (workspace state) and
 * applies the returned `newRootValue` back to the workspace.
 */
export async function invokePropertyValueOperation(
    host: UmbControllerHost,
    request: PropertyValueOperationRequest,
): Promise<PropertyValueOperationResponse> {
    // Cast through the generated request shape — the OpenAPI generator can't model the
    // polymorphic AIPropertyPathSegment (string | object), so we shape the body locally and
    // hand it to the typed service.
    const body = request as unknown as PropertyValueOperationRequestModel;

    const { data, error } = await tryExecute(host, PropertyValueOperationsService.invoke({ body }));

    if (error) {
        return {
            success: false,
            error: {
                code: extractErrorCode(error),
                message: extractErrorMessage(error),
            },
        };
    }

    return data ?? { success: false, error: { code: "no-response", message: "Empty response from server." } };
}

function extractErrorCode(error: unknown): string {
    if (error && typeof error === "object" && "status" in error) {
        const status = (error as { status?: number }).status;
        if (typeof status === "number") {
            return `http-${status}`;
        }
    }
    return "transport-error";
}

function extractErrorMessage(error: unknown): string {
    if (error instanceof Error) {
        return error.message;
    }
    if (error && typeof error === "object" && "message" in error) {
        const m = (error as { message?: unknown }).message;
        if (typeof m === "string") return m;
    }
    return "Unknown transport error";
}
