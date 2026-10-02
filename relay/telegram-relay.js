const TELEGRAM_API = "https://api.telegram.org";
const ROUTE = /^\/bot(\d+):[A-Za-z0-9_-]+\/(getMe|getUpdates|sendMessage)$/;

export default {
  async fetch(request, env) {
    if (request.method !== "GET" && request.method !== "POST") return reject(405, "Method not allowed");

    const { pathname } = new URL(request.url);
    const match = pathname.match(ROUTE);
    if (!match) return reject(404, "Not Found");

    const allowed = (env.ALLOWED_BOT_IDS ?? "").split(",").map((id) => id.trim()).filter(Boolean);
    if (!allowed.includes(match[1])) return reject(400, `Bot ${match[1]} is not allowed on this relay (ALLOWED_BOT_IDS)`);

    let upstream;
    try {
      upstream = await fetch(TELEGRAM_API + pathname, {
        method: request.method,
        headers: { "Content-Type": request.headers.get("Content-Type") ?? "application/json" },
        body: request.method === "POST" ? await request.arrayBuffer() : undefined,
      });
    } catch {
      return reject(502, "Relay could not reach api.telegram.org");
    }

    return new Response(upstream.body, {
      status: upstream.status,
      headers: { "Content-Type": upstream.headers.get("Content-Type") ?? "application/json" },
    });
  },
};

function reject(status, description) {
  return Response.json({ ok: false, error_code: status, description }, { status });
}
