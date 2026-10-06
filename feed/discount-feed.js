const KEY = "feed";
const MAX_BYTES = 2_000_000;
const EMPTY = { version: 0, revision: 0, instanceId: null, contentHash: null, generatedAtUtc: null, programs: [] };

export default {
  async fetch(request, env) {
    const { pathname } = new URL(request.url);
    if (pathname !== "/v1/feed") return reject(404, "Not Found");

    if (request.method === "GET") {
      const role = authorized(request, env.WRITE_TOKEN) ? "write" : authorized(request, env.READ_TOKEN) ? "read" : null;
      if (!role) return reject(401, "Unauthorized");
      const { value, metadata } = await env.FEED.getWithMetadata(KEY);
      return new Response(value ?? JSON.stringify(EMPTY), {
        headers: {
          "Content-Type": "application/json; charset=utf-8",
          "Cache-Control": "no-store",
          ETag: etag(stateOf(metadata).revision),
          "X-Token-Role": role,
        },
      });
    }

    if (request.method === "PUT") {
      if (!authorized(request, env.WRITE_TOKEN)) return reject(401, "Unauthorized");
      const expected = parseIfMatch(request.headers.get("If-Match"));
      if (expected === null) return reject(428, 'If-Match: "<revision>" is required');

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

      const current = stateOf((await env.FEED.getWithMetadata(KEY)).metadata);
      if (expected !== current.revision)
        return Response.json(
          { ok: false, error: "Feed was changed by another writer", current },
          { status: 412, headers: { ETag: etag(current.revision) } },
        );

      const revision = current.revision + 1;
      feed.revision = revision;
      const metadata = { revision, instanceId: feed.instanceId, contentHash: feed.contentHash, generatedAtUtc: feed.generatedAtUtc };
      await env.FEED.put(KEY, JSON.stringify(feed), { metadata });
      return Response.json({ ok: true, revision }, { headers: { ETag: etag(revision) } });
    }

    return reject(405, "Method not allowed");
  },
};

// Feeds written before revisions existed have no metadata: they count as revision 0.
function stateOf(metadata) {
  return {
    revision: Number.isInteger(metadata?.revision) ? metadata.revision : 0,
    instanceId: metadata?.instanceId ?? null,
    contentHash: metadata?.contentHash ?? null,
    generatedAtUtc: metadata?.generatedAtUtc ?? null,
  };
}

function etag(revision) {
  return `"${revision}"`;
}

function parseIfMatch(header) {
  const match = /^"(\d{1,15})"$/.exec((header ?? "").trim());
  return match ? Number(match[1]) : null;
}

function validate(feed) {
  if (!feed || typeof feed !== "object" || !Number.isFinite(feed.version)) return "version is required";
  if (typeof feed.instanceId !== "string" || !/^[A-Za-z0-9-]{1,64}$/.test(feed.instanceId)) return "instanceId is required";
  if (typeof feed.contentHash !== "string" || !/^[A-Fa-f0-9]{1,128}$/.test(feed.contentHash)) return "contentHash is required";
  if (feed.generatedAtUtc != null && Number.isNaN(Date.parse(feed.generatedAtUtc))) return "generatedAtUtc is invalid";
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
