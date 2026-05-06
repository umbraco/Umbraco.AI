import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import type { UmbElement } from "@umbraco-cms/backoffice/element-api";

/**
 * Configures a generated hey-api client for authenticated calls to the
 * Umbraco backoffice Management API.
 *
 * Delegates to `authContext.configureClient(client)`, which:
 * - Sets `baseUrl`, `credentials: 'include'`, and an `auth` callback that
 *   gates each request on `#ensureTokenReady` (inline refresh + cross-tab
 *   Web Lock coordination).
 * - Binds the default response interceptors (401 retry, 403 handling,
 *   error normalization, server notifications) with the auth context's
 *   own host so the `UmbAuthSignalerContext` is registered at `umb-app`
 *   and emissions reach `UmbAuthContext` correctly.
 *
 * @param host The entry point's `host` parameter (`UmbElement`).
 * @param client The generated hey-api client to configure.
 * @returns A Promise that resolves once auth is configured on the client.
 * @public
 */
export function configureAiClient(host: UmbElement, client: unknown): Promise<void> {
    return new Promise<void>((resolve) => {
        host.consumeContext(UMB_AUTH_CONTEXT, (authContext) => {
            if (!authContext) return;
            // Cast: per-project hey-api codegen produces nominally distinct Client
            // types, but configureClient only uses the shared setConfig + interceptor
            // surface that every hey-api Client provides.
            authContext.configureClient(client as never);
            resolve();
        });
    });
}
