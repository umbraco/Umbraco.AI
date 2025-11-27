import { UmbDetailRepositoryBase as d } from "@umbraco-cms/backoffice/repository";
import { tryExecuteAndNotify as o } from "@umbraco-cms/backoffice/resources";
import { C as n, U as a } from "./type-mapper-Dmt5vB2C.js";
import { e as p } from "./bundle.manifests-BL8gZONS.js";
import { UAI_CONNECTION_DETAIL_STORE_CONTEXT as y } from "./connection-detail.store-Du8jOyAs.js";
class l {
  #t;
  constructor(t) {
    this.#t = t;
  }
  /**
   * Creates a scaffold for a new connection.
   */
  async createScaffold(t) {
    return { data: {
      unique: "",
      entityType: p,
      alias: "",
      name: "",
      providerId: t?.providerId ?? "",
      settings: null,
      isActive: !0,
      ...t
    } };
  }
  /**
   * Reads a connection by its unique identifier.
   */
  async read(t) {
    const { data: e, error: r } = await o(
      this.#t,
      n.getConnectionById({ path: { id: t } })
    );
    return r || !e ? { error: r } : { data: a.toDetailModel(e) };
  }
  /**
   * Creates a new connection.
   */
  async create(t, e) {
    const r = a.toCreateRequest(t), { response: c, error: i } = await o(
      this.#t,
      n.postConnection({ body: r })
    );
    if (i)
      return { error: i };
    const u = (c?.headers?.get("Location") ?? "").split("/").pop() ?? "";
    return {
      data: {
        ...t,
        unique: u
      }
    };
  }
  /**
   * Updates an existing connection.
   */
  async update(t) {
    const e = a.toUpdateRequest(t), { error: r } = await o(
      this.#t,
      n.putConnectionById({
        path: { id: t.unique },
        body: e
      })
    );
    return r ? { error: r } : { data: t };
  }
  /**
   * Deletes a connection by its unique identifier.
   */
  async delete(t) {
    const { error: e } = await o(
      this.#t,
      n.deleteConnectionById({ path: { id: t } })
    );
    return e ? { error: e } : {};
  }
}
class N extends d {
  constructor(t) {
    super(t, l, y);
  }
  async create(t) {
    return super.create(t, null);
  }
}
export {
  N as UaiConnectionDetailRepository,
  N as api
};
//# sourceMappingURL=connection-detail.repository-gai7dQ-B.js.map
