import { UmbDetailRepositoryBase as d } from "@umbraco-cms/backoffice/repository";
import { tryExecuteAndNotify as o } from "@umbraco-cms/backoffice/resources";
import { C as a, U as n } from "./type-mapper-G4MX2UFa.js";
import { a as p } from "./bundle.manifests-BwiwDf1o.js";
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
      a.getConnectionById({ path: { id: t } })
    );
    return r || !e ? { error: r } : { data: n.toDetailModel(e) };
  }
  /**
   * Creates a new connection.
   */
  async create(t, e) {
    const r = n.toCreateRequest(t), { response: c, error: i } = await o(
      this.#t,
      a.postConnection({ body: r })
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
    const e = n.toUpdateRequest(t), { error: r } = await o(
      this.#t,
      a.putConnectionById({
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
      a.deleteConnectionById({ path: { id: t } })
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
//# sourceMappingURL=connection-detail.repository-DU_QWilS.js.map
