import { c as i } from "./client.gen-CF69_sVb.js";
import { a as n } from "./bundle.manifests-BwiwDf1o.js";
class c {
  static getConnections(t) {
    return (t?.client ?? i).get({
      security: [
        {
          scheme: "bearer",
          type: "http"
        }
      ],
      url: "/umbraco/ai/management/api/v1/connections",
      ...t
    });
  }
  static postConnection(t) {
    return (t?.client ?? i).post({
      security: [
        {
          scheme: "bearer",
          type: "http"
        }
      ],
      url: "/umbraco/ai/management/api/v1/connections",
      ...t,
      headers: {
        "Content-Type": "application/json",
        ...t?.headers
      }
    });
  }
  static deleteConnectionById(t) {
    return (t.client ?? i).delete({
      security: [
        {
          scheme: "bearer",
          type: "http"
        }
      ],
      url: "/umbraco/ai/management/api/v1/connections/{id}",
      ...t
    });
  }
  static getConnectionById(t) {
    return (t.client ?? i).get({
      security: [
        {
          scheme: "bearer",
          type: "http"
        }
      ],
      url: "/umbraco/ai/management/api/v1/connections/{id}",
      ...t
    });
  }
  static putConnectionById(t) {
    return (t.client ?? i).put({
      security: [
        {
          scheme: "bearer",
          type: "http"
        }
      ],
      url: "/umbraco/ai/management/api/v1/connections/{id}",
      ...t,
      headers: {
        "Content-Type": "application/json",
        ...t.headers
      }
    });
  }
  static postConnectionsByIdTest(t) {
    return (t.client ?? i).post({
      security: [
        {
          scheme: "bearer",
          type: "http"
        }
      ],
      url: "/umbraco/ai/management/api/v1/connections/{id}/test",
      ...t
    });
  }
}
const s = {
  toDetailModel(e) {
    return {
      unique: e.id,
      entityType: n,
      alias: e.alias,
      name: e.name,
      providerId: e.providerId,
      settings: e.settings ?? null,
      isActive: e.isActive
    };
  },
  toItemModel(e) {
    return {
      unique: e.id,
      entityType: n,
      name: e.name,
      providerId: e.providerId,
      isActive: e.isActive
    };
  },
  toCreateRequest(e) {
    return {
      alias: e.alias,
      name: e.name,
      providerId: e.providerId,
      settings: e.settings,
      isActive: e.isActive
    };
  },
  toUpdateRequest(e) {
    return {
      alias: e.alias,
      name: e.name,
      settings: e.settings,
      isActive: e.isActive
    };
  }
};
export {
  c as C,
  s as U
};
//# sourceMappingURL=type-mapper-G4MX2UFa.js.map
