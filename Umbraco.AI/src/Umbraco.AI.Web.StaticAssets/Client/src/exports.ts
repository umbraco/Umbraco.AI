export * from "./chat/exports.js";
export * from "./context/exports.js";
export * from "./core/exports.js";
export * from "./embeddings/exports.js";
export * from "./speech-to-text/exports.js";
export * from "./image-generation/exports.js";
export * from "./entity-adapter/exports.js";
export * from "./profile/exports.js";
export * from "./request-context/exports.js";
export * from "./tool/exports.js";
export * from "./workspace-registry/exports.js";
export * from "./section/exports.js";
export * from "./test/exports.js";
export * from "./constants.js";

// Property value operation API surface — needed by add-on packages (e.g. agent-copilot's
// property value tools) that invoke the dispatcher endpoint via the typed hey-api client.
export {
    PropertyValueOperationsService,
    type AiPropertyOperationModel,
    type AiPropertyValueOperationErrorModel,
    type PropertyValueOperationRequestModel,
    type PropertyValueOperationRequestModelWritable,
    type PropertyValueOperationResponseModel,
    type AiDocumentMetadataModel,
    type AiDocumentMetadataModelWritable,
    type AiVariantIdModel,
    type AiVariantIdModelWritable,
} from "./api/index.js";

// Export client ready promise for nested packages to wait on
export { coreClientReady } from "./app.js";
