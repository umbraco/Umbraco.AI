import { UmbDetailRepositoryBase as d } from "@umbraco-cms/backoffice/repository";
import { tryExecuteAndNotify as r } from "@umbraco-cms/backoffice/resources";
import { C as o, U as a } from "./type-mapper-C18x2v2A.js";
import { U as p } from "./bundle.manifests-BsBZQJMT.js";
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
      entityType: p.EntityType.Entity,
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
    const { data: e, error: n } = await r(
      this.#t,
      o.getConnectionById({ path: { id: t } })
    );
    return n || !e ? { error: n } : { data: a.toDetailModel(e) };
  }
  /**
   * Creates a new connection.
   */
  async create(t, e) {
    const n = a.toCreateRequest(t), { response: c, error: i } = await r(
      this.#t,
      o.postConnection({ body: n })
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
    const e = a.toUpdateRequest(t), { error: n } = await r(
      this.#t,
      o.putConnectionById({
        path: { id: t.unique },
        body: e
      })
    );
    return n ? { error: n } : { data: t };
  }
  /**
   * Deletes a connection by its unique identifier.
   */
  async delete(t) {
    const { error: e } = await r(
      this.#t,
      o.deleteConnectionById({ path: { id: t } })
    );
    return e ? { error: e } : {};
  }
}
class I extends d {
  constructor(t) {
    super(t, l, y);
  }
  async create(t) {
    return super.create(t, null);
  }
}
export {
  I as UaiConnectionDetailRepository,
  I as api
};
//# sourceMappingURL=connection-detail.repository-Bq4gwARE.js.map
