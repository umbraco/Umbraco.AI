import { c as i } from "./client.gen-CF69_sVb.js";
import { U as n } from "./bundle.manifests-D_6geKLR.js";
class r {
  static getConnections(e) {
    return (e?.client ?? i).get({
      security: [
        {
          scheme: "bearer",
          type: "http"
        }
      ],
      url: "/umbraco/ai/management/api/v1/connections",
      ...e
    });
  }
  static postConnections(e) {
    return (e?.client ?? i).post({
      security: [
        {
          scheme: "bearer",
          type: "http"
        }
      ],
      url: "/umbraco/ai/management/api/v1/connections",
      ...e,
      headers: {
        "Content-Type": "application/json",
        ...e?.headers
      }
    });
  }
  static deleteConnectionsById(e) {
    return (e.client ?? i).delete({
      security: [
        {
          scheme: "bearer",
          type: "http"
        }
      ],
      url: "/umbraco/ai/management/api/v1/connections/{id}",
      ...e
    });
  }
  static getConnectionsById(e) {
    return (e.client ?? i).get({
      security: [
        {
          scheme: "bearer",
          type: "http"
        }
      ],
      url: "/umbraco/ai/management/api/v1/connections/{id}",
      ...e
    });
  }
  static putConnectionsById(e) {
    return (e.client ?? i).put({
      security: [
        {
          scheme: "bearer",
          type: "http"
        }
      ],
      url: "/umbraco/ai/management/api/v1/connections/{id}",
      ...e,
      headers: {
        "Content-Type": "application/json",
        ...e.headers
      }
    });
  }
  static postConnectionsByIdTest(e) {
    return (e.client ?? i).post({
      security: [
        {
          scheme: "bearer",
          type: "http"
        }
      ],
      url: "/umbraco/ai/management/api/v1/connections/{id}/test",
      ...e
    });
  }
}
const s = {
  toDetailModel(t) {
    return {
      unique: t.id,
      entityType: n.EntityType.Entity,
      alias: t.alias,
      name: t.name,
      providerId: t.providerId,
      settings: t.settings ?? null,
      isActive: t.isActive
    };
  },
  toItemModel(t) {
    return {
      unique: t.id,
      entityType: n.EntityType.Entity,
      name: t.name,
      providerId: t.providerId,
      isActive: t.isActive
    };
  },
  toCreateRequest(t) {
    return {
      alias: t.alias,
      name: t.name,
      providerId: t.providerId,
      settings: t.settings,
      isActive: t.isActive
    };
  },
  toUpdateRequest(t) {
    return {
      alias: t.alias,
      name: t.name,
      settings: t.settings,
      isActive: t.isActive
    };
  }
};
export {
  r as C,
  s as U
};
//# sourceMappingURL=type-mapper-C2YdcGsS.js.map
