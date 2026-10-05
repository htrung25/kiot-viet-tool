const KEY = "feed";
const EMPTY = JSON.stringify({ version: 0, generatedAtUtc: null, programs: [] });
const MAX_BYTES = 2_000_000;

export default {
  async fetch(request, env) {
    const { pathname } = new URL(request.url);
    if (pathname !== "/v1/feed") return reject(404, "Not Found");

    if (request.method === "GET") {
      if (!authorized(request, env.READ_TOKEN) && !authorized(request, env.WRITE_TOKEN)) return reject(401, "Unauthorized");
      return new Response((await env.FEED.get(KEY)) ?? EMPTY, {
        headers: { "Content-Type": "application/json; charset=utf-8", "Cache-Control": "no-store" },
      });
    }

    if (request.method === "PUT") {
      if (!authorized(request, env.WRITE_TOKEN)) return reject(401, "Unauthorized");
      const body = await request.text();
      if (body.length > MAX_BYTES) return reject(413, "Feed too large");
      let feed;
      try {
        feed = JSON.parse(body);
      } catch {
        return reject(400, "Body is not JSON");
      }
      const problem = validate(feed);
      if (problem) return reject(400, problem);
      await env.FEED.put(KEY, body);
      return Response.json({ ok: true, version: feed.version });
    }

    return reject(405, "Method not allowed");
  },
};

function validate(feed) {
  if (!feed || typeof feed !== "object" || !Number.isFinite(feed.version)) return "version is required";
  if (!Array.isArray(feed.programs)) return "programs must be an array";
  for (const p of feed.programs) {
    if (!Number.isInteger(p.id)) return "program id must be an integer";
    if (p.type !== "percent" && p.type !== "amount") return `program ${p.id}: type must be percent or amount`;
    if (!(p.value > 0) || (p.type === "percent" && p.value >= 100)) return `program ${p.id}: invalid value`;
    if (Number.isNaN(Date.parse(p.startAtUtc)) || Number.isNaN(Date.parse(p.endAtUtc))) return `program ${p.id}: invalid dates`;
    if (!Array.isArray(p.productIds) || !p.productIds.every(Number.isInteger)) return `program ${p.id}: productIds must be integers`;
  }
  return null;
}

function authorized(request, expected) {
  const header = request.headers.get("Authorization") ?? "";
  if (!expected || !header.startsWith("Bearer ")) return false;
  const a = new TextEncoder().encode(header.slice(7));
  const b = new TextEncoder().encode(expected);
  return a.byteLength === b.byteLength && crypto.subtle.timingSafeEqual(a, b);
}

function reject(status, error) {
  return Response.json({ ok: false, error }, { status });
}
