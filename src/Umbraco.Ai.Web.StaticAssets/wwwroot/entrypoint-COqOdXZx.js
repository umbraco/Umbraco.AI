import { UMB_AUTH_CONTEXT as i } from "@umbraco-cms/backoffice/auth";
import { c as s } from "./client.gen-Ce7o8kG8.js";
const c = (o, t) => {
  console.log("Umbraco AI Entrypoint initialized"), o.consumeContext(i, async (e) => {
    const n = e?.getOpenApiConfiguration();
    s.setConfig({
      auth: n?.token ?? void 0,
      baseUrl: n?.base ?? "",
      credentials: n?.credentials ?? "same-origin"
    });
  });
}, g = (o, t) => {
};
export {
  c as onInit,
  g as onUnload
};
//# sourceMappingURL=entrypoint-COqOdXZx.js.map
