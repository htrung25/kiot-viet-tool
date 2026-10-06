(function (root) {
  const FRESH_MS = 30 * 60 * 1000;
  const HIGH_DISCOUNT_RATIO = 0.5; 
  const TOTAL_FIELDS = ["SubTotal", "Total"];
  const CHANGED = "Cấu trúc hoá đơn KiotViet đã thay đổi, tiện ích tạm không giảm giá";

  function isLineArray(items) {
    const first = Array.isArray(items) && items.length > 0 ? items[0] : null;
    return !!first && typeof first === "object" && Number.isFinite(first.ProductId) && Number.isFinite(first.Quantity);
  }

  function linePrice(x) {
    return Number.isFinite(x.Price) ? x.Price : Number(x.BasePrice);
  }

  function readLines(cart) {
    for (const key of Object.keys(cart || {})) {
      const items = cart[key];
      if (!isLineArray(items)) continue;
      return {
        key,
        lines: items.map((x) => ({ productId: x.ProductId, quantity: Number(x.Quantity), price: linePrice(x) })),
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

  function subtotal(lines) {
    return lines.reduce((sum, l) => sum + (l.quantity > 0 && l.price > 0 ? l.price * l.quantity : 0), 0);
  }

  // Null when the cart looks like the one the extension was written for; otherwise why nothing is applied.
  function validateCart(cart) {
    const arrays = Object.keys(cart || {}).filter((k) => isLineArray(cart[k]));
    if (arrays.length === 0) return "Không đọc được dòng hàng của hoá đơn.";
    if (arrays.length > 1) return `${CHANGED} (có ${arrays.length} danh sách hàng).`;

    for (const x of cart[arrays[0]]) {
      if (!x || typeof x !== "object" || !Number.isInteger(x.ProductId) || x.ProductId <= 0) return `${CHANGED} (mã hàng không hợp lệ).`;
      if (!Number.isFinite(x.Quantity) || x.Quantity < 0) return `${CHANGED} (số lượng không hợp lệ).`;
      const price = linePrice(x);
      if (!Number.isFinite(price) || price < 0) return `${CHANGED} (đơn giá không hợp lệ).`;
    }

    // A changed meaning of Price shows up as lines that no longer add up to the cart total.
    const sum = subtotal(readLines(cart).lines);
    const discounts = [0, Number(cart.Discount) || 0, (Number(cart.Discount) || 0) + (Number(cart.DiscountByPromotionValue) || 0)];
    const totals = TOTAL_FIELDS.map((f) => cart[f]).filter(Number.isFinite);
    if (totals.length > 0 && !totals.some((t) => discounts.some((d) => Math.abs(sum - d - t) <= 1)))
      return `${CHANGED} (tổng tiền hàng không khớp giá từng dòng).`;
    return null;
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
    const problem = feedProblem(feedState, machineNowMs) || validateCart(cart);
    if (problem) return { action: "set", amount: 0, reason: problem };
    const read = readLines(cart);
    const amount = computeDiscount(read.lines, feedState.feed.programs, machineNowMs + (feedState.offsetMs || 0));
    const goods = subtotal(read.lines);
    return {
      action: "set",
      amount,
      signature: signature(read.lines),
      confirm: amount > 0 && amount > goods * HIGH_DISCOUNT_RATIO,
      subtotal: goods,
    };
  }

  root.KvtDiscount = { readLines, computeDiscount, subtotal, validateCart, signature, feedProblem, decide, FRESH_MS, HIGH_DISCOUNT_RATIO };
  if (typeof module !== "undefined") module.exports = root.KvtDiscount;
})(typeof window !== "undefined" ? window : globalThis);
