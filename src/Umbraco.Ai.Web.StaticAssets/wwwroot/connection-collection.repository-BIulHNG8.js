import { UmbRepositoryBase as i } from "@umbraco-cms/backoffice/repository";
import { tryExecuteAndNotify as r } from "@umbraco-cms/backoffice/resources";
import { C as s, U as a } from "./type-mapper-C18x2v2A.js";
class c {
  #t;
  constructor(t) {
    this.#t = t;
  }
  /**
   * Gets all connections as collection items.
   */
  async getCollection(t) {
    const { data: o, error: e } = await r(
      this.#t,
      s.getConnections({
        query: {
          skip: t.skip ?? 0,
          take: t.take ?? 100
        }
      })
    );
    return e || !o ? { error: e } : {
      data: {
        items: o.items.map(a.toItemModel),
        total: o.total
      }
    };
  }
}
class C extends i {
  #t;
  constructor(t) {
    super(t), this.#t = new c(t);
  }
  async requestCollection(t) {
    return this.#t.getCollection(t);
  }
}
export {
  C as UaiConnectionCollectionRepository,
  C as api
};
//# sourceMappingURL=connection-collection.repository-BIulHNG8.js.map
