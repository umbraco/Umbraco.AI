import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import type { UmbApiClient } from "@umbraco-cms/backoffice/http-client";
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
 * Additionally sets `throwOnError: true` — every product sharing this function
 * relies on `tryExecute` to turn a failed request into a user-facing notification,
 * which only happens when the underlying call throws. `setConfig` merges onto the
 * config `authContext.configureClient` already applied, so this doesn't clobber
 * `baseUrl`/`credentials`/`auth`.
 *
 * @param host The entry point's `host` parameter (`UmbElement`).
 * @param client The generated hey-api client to configure.
 * @returns A Promise that resolves once auth is configured on the client.
 * @public
 */
export function configureAiClient(host: UmbElement, client: UmbApiClient): Promise<void> {
    return new Promise<void>((resolve) => {
        host.consumeContext(UMB_AUTH_CONTEXT, (authContext) => {
            if (!authContext) return;
            authContext.configureClient(client);
            client.setConfig({ throwOnError: true });
            resolve();
        });
    });
}
