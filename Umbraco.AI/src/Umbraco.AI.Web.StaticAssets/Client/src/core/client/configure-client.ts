import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import type { UmbElement } from "@umbraco-cms/backoffice/element-api";

// Permissive structural type — each generated hey-api client is nominally
// distinct per-project, but they all share `setConfig` and the response
// interceptor surface used here.
// eslint-disable-next-line @typescript-eslint/no-explicit-any
interface AiHttpClient {
    setConfig: (config: any) => unknown;
    interceptors: {
        response: {
            use: (
                fn: (
                    response: Response,
                    request: Request,
                    opts?: unknown,
                ) => Response | Promise<Response>,
            ) => unknown;
        };
    };
}

/**
 * Configures a generated hey-api client with the backoffice auth callback
 * and attaches a silent 401 recovery interceptor.
 *
 * The interceptor calls `authContext.makeRefreshTokenRequest()` on a 401
 * and retries the request once. This is a workaround for CMS issue
 * https://github.com/umbraco/Umbraco-CMS/issues/22647 — the default
 * `UmbApiInterceptorController.bindDefaultInterceptors` is unusable from
 * third-party entry-point hosts because its `UmbAuthSignalerContext`
 * ends up scoped below `umb-app` and emissions never reach `UmbAuthContext`.
 *
 * @param host The entry point's `host` parameter (`UmbElement`).
 * @param client The generated hey-api client to configure.
 * @returns A Promise that resolves once auth is configured on the client.
 * @public
 */
export function configureAiClient(host: UmbElement, client: AiHttpClient): Promise<void> {
    return new Promise<void>((resolve) => {
        host.consumeContext(UMB_AUTH_CONTEXT, async (authContext) => {
            if (!authContext) return;

            const config = authContext.getOpenApiConfiguration();
            client.setConfig({
                auth: config?.token ?? undefined,
                baseUrl: config?.base ?? "",
                credentials: config?.credentials ?? "same-origin",
            });

            const retried = new WeakSet<Request>();
            client.interceptors.response.use(async (response, request) => {
                if (response.status !== 401) return response;
                if (retried.has(request)) return response;
                if (request.method !== "GET" && request.method !== "HEAD") return response;

                const refreshed = await authContext
                    .makeRefreshTokenRequest()
                    .catch(() => false);
                if (!refreshed) return response;

                const retryRequest = new Request(request.url, {
                    method: request.method,
                    headers: new Headers(request.headers),
                    credentials: request.credentials,
                    cache: request.cache,
                    redirect: request.redirect,
                });
                retried.add(retryRequest);
                return fetch(retryRequest);
            });

            resolve();
        });
    });
}
