(function (root) {
  const FRESH_MS = 30 * 60 * 1000;

  function readLines(cart) {
    for (const key of Object.keys(cart || {})) {
      const items = cart[key];
      if (!Array.isArray(items) || items.length === 0) continue;
      const first = items[0];
      if (!first || typeof first !== "object" || !Number.isFinite(first.ProductId) || !Number.isFinite(first.Quantity)) continue;
      return {
        key,
        lines: items.map((x) => ({
          productId: x.ProductId,
          quantity: Number(x.Quantity),
          price: Number.isFinite(x.Price) ? x.Price : Number(x.BasePrice),
        })),
      };
    }
    return null;
  }

  // Must stay identical to DiscountFeedSnapshot.Evaluate (C#), used to reconcile invoices.
  function computeDiscount(lines, programs, nowMs) {
    const byProduct = new Map();
    for (const p of programs || []) {
      if (!(Date.parse(p.startAtUtc) <= nowMs && nowMs < Date.parse(p.endAtUtc))) continue;
      for (const id of p.productIds) if (!byProduct.has(id)) byProduct.set(id, p);
    }
    let total = 0;
    for (const line of lines) {
      const p = byProduct.get(line.productId);
      if (!p || !(line.quantity > 0) || !(line.price > 0)) continue;
      const perUnit = p.type === "percent" ? (line.price * p.value) / 100 : p.value < line.price ? p.value : 0;
      total += perUnit * line.quantity;
    }
    return Math.round(total * 100) / 100;
  }

  function signature(lines) {
    return lines.map((l) => `${l.productId}:${l.quantity}:${l.price}`).join("|");
  }

  function feedProblem(feedState, nowMs) {
    if (!feedState || !feedState.configured) return "Chưa cấu hình tiện ích (bấm biểu tượng tiện ích để nhập địa chỉ và mã).";
    if (!feedState.fetchedAt) return feedState.error || "Chưa tải được danh sách giảm giá.";
    if (nowMs - feedState.fetchedAt > FRESH_MS)
      return "Danh sách giảm giá đã cũ hơn 30 phút" + (feedState.error ? `: ${feedState.error}` : ".");
    return null;
  }

  function decide(cart, mine, feedState, machineNowMs) {
    const current = Number(cart.Discount) || 0;
    if (current > 0 && current !== mine) return { action: "skip", reason: "Hoá đơn đã có giảm giá nhập tay." };
    const problem = feedProblem(feedState, machineNowMs);
    if (problem) return { action: "set", amount: 0, reason: problem };
    const read = readLines(cart);
    if (!read) return { action: "set", amount: 0, reason: "Không đọc được dòng hàng của hoá đơn." };
    const amount = computeDiscount(read.lines, feedState.feed.programs, machineNowMs + (feedState.offsetMs || 0));
    return { action: "set", amount, signature: signature(read.lines) };
  }

  root.KvtDiscount = { readLines, computeDiscount, signature, feedProblem, decide, FRESH_MS };
  if (typeof module !== "undefined") module.exports = root.KvtDiscount;
})(typeof window !== "undefined" ? window : globalThis);
