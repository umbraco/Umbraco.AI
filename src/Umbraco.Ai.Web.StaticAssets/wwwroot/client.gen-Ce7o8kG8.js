const L = {
  bodySerializer: (r) => JSON.stringify(
    r,
    (t, e) => typeof e == "bigint" ? e.toString() : e
  )
}, V = ({
  onSseError: r,
  onSseEvent: t,
  responseTransformer: e,
  responseValidator: c,
  sseDefaultRetryDelay: a,
  sseMaxRetryAttempts: l,
  sseMaxRetryDelay: o,
  sseSleepFn: n,
  url: i,
  ...f
}) => {
  let s;
  const h = n ?? ((j) => new Promise((u) => setTimeout(u, j)));
  return { stream: async function* () {
    let j = a ?? 3e3, u = 0;
    const b = f.signal ?? new AbortController().signal;
    for (; !b.aborted; ) {
      u++;
      const E = f.headers instanceof Headers ? f.headers : new Headers(f.headers);
      s !== void 0 && E.set("Last-Event-ID", s);
      try {
        const y = await fetch(i, { ...f, headers: E, signal: b });
        if (!y.ok)
          throw new Error(
            `SSE failed: ${y.status} ${y.statusText}`
          );
        if (!y.body) throw new Error("No body in SSE response");
        const g = y.body.pipeThrough(new TextDecoderStream()).getReader();
        let m = "";
        const d = () => {
          try {
            g.cancel();
          } catch {
          }
        };
        b.addEventListener("abort", d);
        try {
          for (; ; ) {
            const { done: w, value: H } = await g.read();
            if (w) break;
            m += H;
            const $ = m.split(`

`);
            m = $.pop() ?? "";
            for (const v of $) {
              const B = v.split(`
`), A = [];
              let T;
              for (const p of B)
                if (p.startsWith("data:"))
                  A.push(p.replace(/^data:\s*/, ""));
                else if (p.startsWith("event:"))
                  T = p.replace(/^event:\s*/, "");
                else if (p.startsWith("id:"))
                  s = p.replace(/^id:\s*/, "");
                else if (p.startsWith("retry:")) {
                  const k = Number.parseInt(
                    p.replace(/^retry:\s*/, ""),
                    10
                  );
                  Number.isNaN(k) || (j = k);
                }
              let x, I = !1;
              if (A.length) {
                const p = A.join(`
`);
                try {
                  x = JSON.parse(p), I = !0;
                } catch {
                  x = p;
                }
              }
              I && (c && await c(x), e && (x = await e(x))), t?.({
                data: x,
                event: T,
                id: s,
                retry: j
              }), A.length && (yield x);
            }
          }
        } finally {
          b.removeEventListener("abort", d), g.releaseLock();
        }
        break;
      } catch (y) {
        if (r?.(y), l !== void 0 && u >= l)
          break;
        const g = Math.min(
          j * 2 ** (u - 1),
          o ?? 3e4
        );
        await h(g);
      }
    }
  }() };
}, R = async (r, t) => {
  const e = typeof t == "function" ? await t(r) : t;
  if (e)
    return r.scheme === "bearer" ? `Bearer ${e}` : r.scheme === "basic" ? `Basic ${btoa(e)}` : e;
}, J = (r) => {
  switch (r) {
    case "label":
      return ".";
    case "matrix":
      return ";";
    case "simple":
      return ",";
    default:
      return "&";
  }
}, F = (r) => {
  switch (r) {
    case "form":
      return ",";
    case "pipeDelimited":
      return "|";
    case "spaceDelimited":
      return "%20";
    default:
      return ",";
  }
}, G = (r) => {
  switch (r) {
    case "label":
      return ".";
    case "matrix":
      return ";";
    case "simple":
      return ",";
    default:
      return "&";
  }
}, P = ({
  allowReserved: r,
  explode: t,
  name: e,
  style: c,
  value: a
}) => {
  if (!t) {
    const n = (r ? a : a.map((i) => encodeURIComponent(i))).join(F(c));
    switch (c) {
      case "label":
        return `.${n}`;
      case "matrix":
        return `;${e}=${n}`;
      case "simple":
        return n;
      default:
        return `${e}=${n}`;
    }
  }
  const l = J(c), o = a.map((n) => c === "label" || c === "simple" ? r ? n : encodeURIComponent(n) : C({
    allowReserved: r,
    name: e,
    value: n
  })).join(l);
  return c === "label" || c === "matrix" ? l + o : o;
}, C = ({
  allowReserved: r,
  name: t,
  value: e
}) => {
  if (e == null)
    return "";
  if (typeof e == "object")
    throw new Error(
      "Deeply-nested arrays/objects aren’t supported. Provide your own `querySerializer()` to handle these."
    );
  return `${t}=${r ? e : encodeURIComponent(e)}`;
}, U = ({
  allowReserved: r,
  explode: t,
  name: e,
  style: c,
  value: a,
  valueOnly: l
}) => {
  if (a instanceof Date)
    return l ? a.toISOString() : `${e}=${a.toISOString()}`;
  if (c !== "deepObject" && !t) {
    let i = [];
    Object.entries(a).forEach(([s, h]) => {
      i = [
        ...i,
        s,
        r ? h : encodeURIComponent(h)
      ];
    });
    const f = i.join(",");
    switch (c) {
      case "form":
        return `${e}=${f}`;
      case "label":
        return `.${f}`;
      case "matrix":
        return `;${e}=${f}`;
      default:
        return f;
    }
  }
  const o = G(c), n = Object.entries(a).map(
    ([i, f]) => C({
      allowReserved: r,
      name: c === "deepObject" ? `${e}[${i}]` : i,
      value: f
    })
  ).join(o);
  return c === "label" || c === "matrix" ? o + n : n;
}, M = /\{[^{}]+\}/g, Q = ({ path: r, url: t }) => {
  let e = t;
  const c = t.match(M);
  if (c)
    for (const a of c) {
      let l = !1, o = a.substring(1, a.length - 1), n = "simple";
      o.endsWith("*") && (l = !0, o = o.substring(0, o.length - 1)), o.startsWith(".") ? (o = o.substring(1), n = "label") : o.startsWith(";") && (o = o.substring(1), n = "matrix");
      const i = r[o];
      if (i == null)
        continue;
      if (Array.isArray(i)) {
        e = e.replace(
          a,
          P({ explode: l, name: o, style: n, value: i })
        );
        continue;
      }
      if (typeof i == "object") {
        e = e.replace(
          a,
          U({
            explode: l,
            name: o,
            style: n,
            value: i,
            valueOnly: !0
          })
        );
        continue;
      }
      if (n === "matrix") {
        e = e.replace(
          a,
          `;${C({
            name: o,
            value: i
          })}`
        );
        continue;
      }
      const f = encodeURIComponent(
        n === "label" ? `.${i}` : i
      );
      e = e.replace(a, f);
    }
  return e;
}, K = ({
  baseUrl: r,
  path: t,
  query: e,
  querySerializer: c,
  url: a
}) => {
  const l = a.startsWith("/") ? a : `/${a}`;
  let o = (r ?? "") + l;
  t && (o = Q({ path: t, url: o }));
  let n = e ? c(e) : "";
  return n.startsWith("?") && (n = n.substring(1)), n && (o += `?${n}`), o;
}, _ = ({
  allowReserved: r,
  array: t,
  object: e
} = {}) => (a) => {
  const l = [];
  if (a && typeof a == "object")
    for (const o in a) {
      const n = a[o];
      if (n != null)
        if (Array.isArray(n)) {
          const i = P({
            allowReserved: r,
            explode: !0,
            name: o,
            style: "form",
            value: n,
            ...t
          });
          i && l.push(i);
        } else if (typeof n == "object") {
          const i = U({
            allowReserved: r,
            explode: !0,
            name: o,
            style: "deepObject",
            value: n,
            ...e
          });
          i && l.push(i);
        } else {
          const i = C({
            allowReserved: r,
            name: o,
            value: n
          });
          i && l.push(i);
        }
    }
  return l.join("&");
}, X = (r) => {
  if (!r)
    return "stream";
  const t = r.split(";")[0]?.trim();
  if (t) {
    if (t.startsWith("application/json") || t.endsWith("+json"))
      return "json";
    if (t === "multipart/form-data")
      return "formData";
    if (["application/", "audio/", "image/", "video/"].some(
      (e) => t.startsWith(e)
    ))
      return "blob";
    if (t.startsWith("text/"))
      return "text";
  }
}, Y = (r, t) => t ? !!(r.headers.has(t) || r.query?.[t] || r.headers.get("Cookie")?.includes(`${t}=`)) : !1, Z = async ({
  security: r,
  ...t
}) => {
  for (const e of r) {
    if (Y(t, e.name))
      continue;
    const c = await R(e, t.auth);
    if (!c)
      continue;
    const a = e.name ?? "Authorization";
    switch (e.in) {
      case "query":
        t.query || (t.query = {}), t.query[a] = c;
        break;
      case "cookie":
        t.headers.append("Cookie", `${a}=${c}`);
        break;
      case "header":
      default:
        t.headers.set(a, c);
        break;
    }
  }
}, q = (r) => K({
  baseUrl: r.baseUrl,
  path: r.path,
  query: r.query,
  querySerializer: typeof r.querySerializer == "function" ? r.querySerializer : _(r.querySerializer),
  url: r.url
}), N = (r, t) => {
  const e = { ...r, ...t };
  return e.baseUrl?.endsWith("/") && (e.baseUrl = e.baseUrl.substring(0, e.baseUrl.length - 1)), e.headers = D(r.headers, t.headers), e;
}, D = (...r) => {
  const t = new Headers();
  for (const e of r) {
    if (!e || typeof e != "object")
      continue;
    const c = e instanceof Headers ? e.entries() : Object.entries(e);
    for (const [a, l] of c)
      if (l === null)
        t.delete(a);
      else if (Array.isArray(l))
        for (const o of l)
          t.append(a, o);
      else l !== void 0 && t.set(
        a,
        typeof l == "object" ? JSON.stringify(l) : l
      );
  }
  return t;
};
class O {
  constructor() {
    this._fns = [];
  }
  clear() {
    this._fns = [];
  }
  getInterceptorIndex(t) {
    return typeof t == "number" ? this._fns[t] ? t : -1 : this._fns.indexOf(t);
  }
  exists(t) {
    const e = this.getInterceptorIndex(t);
    return !!this._fns[e];
  }
  eject(t) {
    const e = this.getInterceptorIndex(t);
    this._fns[e] && (this._fns[e] = null);
  }
  update(t, e) {
    const c = this.getInterceptorIndex(t);
    return this._fns[c] ? (this._fns[c] = e, t) : !1;
  }
  use(t) {
    return this._fns = [...this._fns, t], this._fns.length - 1;
  }
}
const ee = () => ({
  error: new O(),
  request: new O(),
  response: new O()
}), te = _({
  allowReserved: !1,
  array: {
    explode: !0,
    style: "form"
  },
  object: {
    explode: !0,
    style: "deepObject"
  }
}), re = {
  "Content-Type": "application/json"
}, W = (r = {}) => ({
  ...L,
  headers: re,
  parseAs: "auto",
  querySerializer: te,
  ...r
}), se = (r = {}) => {
  let t = N(W(), r);
  const e = () => ({ ...t }), c = (f) => (t = N(t, f), e()), a = ee(), l = async (f) => {
    const s = {
      ...t,
      ...f,
      fetch: f.fetch ?? t.fetch ?? globalThis.fetch,
      headers: D(t.headers, f.headers),
      serializedBody: void 0
    };
    s.security && await Z({
      ...s,
      security: s.security
    }), s.requestValidator && await s.requestValidator(s), s.body && s.bodySerializer && (s.serializedBody = s.bodySerializer(s.body)), (s.serializedBody === void 0 || s.serializedBody === "") && s.headers.delete("Content-Type");
    const h = q(s);
    return { opts: s, url: h };
  }, o = async (f) => {
    const { opts: s, url: h } = await l(f), z = {
      redirect: "follow",
      ...s,
      body: s.serializedBody
    };
    let S = new Request(h, z);
    for (const d of a.request._fns)
      d && (S = await d(S, s));
    const j = s.fetch;
    let u = await j(S);
    for (const d of a.response._fns)
      d && (u = await d(u, S, s));
    const b = {
      request: S,
      response: u
    };
    if (u.ok) {
      if (u.status === 204 || u.headers.get("Content-Length") === "0")
        return s.responseStyle === "data" ? {} : {
          data: {},
          ...b
        };
      const d = (s.parseAs === "auto" ? X(u.headers.get("Content-Type")) : s.parseAs) ?? "json";
      let w;
      switch (d) {
        case "arrayBuffer":
        case "blob":
        case "formData":
        case "json":
        case "text":
          w = await u[d]();
          break;
        case "stream":
          return s.responseStyle === "data" ? u.body : {
            data: u.body,
            ...b
          };
      }
      return d === "json" && (s.responseValidator && await s.responseValidator(w), s.responseTransformer && (w = await s.responseTransformer(w))), s.responseStyle === "data" ? w : {
        data: w,
        ...b
      };
    }
    const E = await u.text();
    let y;
    try {
      y = JSON.parse(E);
    } catch {
    }
    const g = y ?? E;
    let m = g;
    for (const d of a.error._fns)
      d && (m = await d(g, u, S, s));
    if (m = m || {}, s.throwOnError)
      throw m;
    return s.responseStyle === "data" ? void 0 : {
      error: m,
      ...b
    };
  }, n = (f) => (s) => o({ ...s, method: f }), i = (f) => async (s) => {
    const { opts: h, url: z } = await l(s);
    return V({
      ...h,
      body: h.body,
      headers: h.headers,
      method: f,
      url: z
    });
  };
  return {
    buildUrl: q,
    connect: n("CONNECT"),
    delete: n("DELETE"),
    get: n("GET"),
    getConfig: e,
    head: n("HEAD"),
    interceptors: a,
    options: n("OPTIONS"),
    patch: n("PATCH"),
    post: n("POST"),
    put: n("PUT"),
    request: o,
    setConfig: c,
    sse: {
      connect: i("CONNECT"),
      delete: i("DELETE"),
      get: i("GET"),
      head: i("HEAD"),
      options: i("OPTIONS"),
      patch: i("PATCH"),
      post: i("POST"),
      put: i("PUT"),
      trace: i("TRACE")
    },
    trace: n("TRACE")
  };
}, ne = se(W({
  baseUrl: "https://localhost:44389"
}));
export {
  ne as c
};
//# sourceMappingURL=client.gen-Ce7o8kG8.js.map
