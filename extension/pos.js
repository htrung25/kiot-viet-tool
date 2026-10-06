(function () {
  const K = window.KvtDiscount;
  const FINGERPRINT_KEY = "kvt-adjust-discount-fingerprint";
  const CHANGED_WARNING_MS = 24 * 60 * 60 * 1000;
  const applied = new WeakMap();
  const signatures = new WeakMap();
  const answered = new WeakMap(); // cart -> { signature, accepted } for the high-discount question
  let feedState = null;
  let lastIssue = null;
  let pageWarning = null;
  let watching = false;

  window.addEventListener("message", (event) => {
    if (event.source !== window || !event.data || event.data.__kvtFeed !== true) return;
    feedState = event.data;
    render();
  });
  window.postMessage({ __kvtHello: true }, location.origin);

  function angularRoot() {
    if (!window.angular) return null;
    const injector = window.angular.element(document.querySelector("[ng-app]") || document.body).injector();
    return injector ? injector.get("$rootScope") : null;
  }

  function paymentController() {
    for (let node = document.querySelector("button.form-discount"); node; node = node.parentElement) {
      const data = window.angular.element(node).data();
      for (const key in data) if (data[key] && typeof data[key].adjustDiscount === "function") return data[key];
    }
    return null;
  }

  function controllerProblem(controller) {
    if (!controller) return "Không tìm thấy ô Giảm giá của KiotViet (có thể KiotViet vừa cập nhật giao diện).";
    if (controller.adjustDiscount.length !== 1) return "Hàm giảm giá của KiotViet đã thay đổi, tiện ích tạm không giảm giá.";
    return null;
  }

  // A different adjustDiscount source means KiotViet shipped a new POS: keep working but show a warning for a day.
  function checkFingerprint(controller) {
    const source = String(controller.adjustDiscount);
    let hash = 2166136261;
    for (let i = 0; i < source.length; i++) hash = Math.imul(hash ^ source.charCodeAt(i), 16777619) >>> 0;
    try {
      const saved = JSON.parse(localStorage.getItem(FINGERPRINT_KEY) || "null");
      if (!saved || saved.hash !== hash) {
        localStorage.setItem(FINGERPRINT_KEY, JSON.stringify({ hash, since: Date.now(), changed: !!saved }));
        pageWarning = saved ? "KiotViet vừa cập nhật trang bán hàng. Kiểm tra vài hoá đơn đầu xem giảm giá có đúng không." : null;
      } else {
        pageWarning = saved.changed && Date.now() - saved.since < CHANGED_WARNING_MS
          ? "KiotViet vừa cập nhật trang bán hàng. Kiểm tra vài hoá đơn đầu xem giảm giá có đúng không."
          : null;
      }
    } catch {
      pageWarning = null; // storage blocked: the check is only a warning
    }
  }

  function writeDiscount(root, cart, amount) {
    const controller = paymentController();
    const problem = controllerProblem(controller);
    if (problem) throw new Error(problem);
    const run = () =>
      controller.adjustDiscount({
        DiscountValue: amount,
        DiscountRatio: 0,
        DiscountByPromotion: cart.DiscountByPromotion,
        DiscountByPromotionValue: cart.DiscountByPromotionValue,
        DiscountByPromotionRatio: cart.DiscountByPromotionRatio,
      });
    if (root.$$phase) run();
    else root.$apply(run);
  }

  // Writes, then reads back: if KiotViet did not take the amount, put the invoice back to no discount.
  function writeVerified(root, cart, amount) {
    writeDiscount(root, cart, amount);
    const now = Number(cart.Discount) || 0;
    if (Math.abs(now - amount) <= 0.01) return;
    try {
      writeDiscount(root, cart, 0);
    } catch {
      // the error below already tells the cashier
    }
    throw new Error(`KiotViet không nhận số giảm giá (đã ghi ${amount}, đọc lại ${now}). Tiện ích đã đặt lại về 0.`);
  }

  function confirmHighDiscount(cart, decision) {
    const previous = answered.get(cart);
    if (previous && previous.signature === decision.signature) return previous.accepted;
    const accepted = window.confirm(
      `KiotViet Tool sắp giảm ${decision.amount.toLocaleString("vi-VN")} ₫, hơn ${Math.round(K.HIGH_DISCOUNT_RATIO * 100)}% ` +
        `tiền hàng (${decision.subtotal.toLocaleString("vi-VN")} ₫).\n\nBấm OK để áp dụng, Cancel để thanh toán không giảm giá.`,
    );
    answered.set(cart, { signature: decision.signature, accepted });
    return accepted;
  }

  function applyAtCheckout() {
    const root = angularRoot();
    const cart = root && root.activeCart;
    if (!cart) return;
    const mine = applied.get(cart) || 0;
    const decision = K.decide(cart, mine, feedState, Date.now());
    lastIssue = decision.reason || null;
    if (decision.action === "skip") return render();
    let amount = decision.amount;
    if (decision.confirm && !confirmHighDiscount(cart, decision)) {
      amount = 0;
      lastIssue = "Thu ngân đã bỏ qua mức giảm lớn cho hoá đơn này.";
    }
    const current = Number(cart.Discount) || 0;
    if (amount === mine && current === mine) return render();
    if (amount === 0 && mine === 0) return render();
    try {
      writeVerified(root, cart, amount);
      applied.set(cart, amount);
      signatures.set(cart, decision.signature);
      console.info("[KiotViet Tool] Giảm giá hoá đơn:", amount);
    } catch (error) {
      applied.delete(cart);
      lastIssue = error.message;
      console.error("[KiotViet Tool]", error);
    }
    render();
  }

  function resetIfCartChanged() {
    const root = angularRoot();
    const cart = root && root.activeCart;
    if (!cart) return;
    const mine = applied.get(cart) || 0;
    if (mine === 0 || Number(cart.Discount) !== mine) return;
    const read = K.readLines(cart);
    if (read && K.signature(read.lines) === signatures.get(cart)) return;
    try {
      writeDiscount(root, cart, 0);
      applied.delete(cart);
    } catch (error) {
      lastIssue = error.message;
    }
  }

  function isCheckoutTrigger(event) {
    if (event.type === "keydown") return event.key === "F9";
    if (!(event.target instanceof Element)) return false;
    if (event.target.closest("payment-invoice-component")) return true;
    const button = event.target.closest("button, a, .btn");
    const text = button ? button.textContent.normalize("NFC").trim().toUpperCase() : "";
    return text === "THANH TOÁN" || /\bQR\b/.test(text);
  }

  function onTrigger(event) {
    if (!isCheckoutTrigger(event)) return;
    try {
      applyAtCheckout();
    } catch (error) {
      lastIssue = error.message;
      console.error("[KiotViet Tool]", error);
      render();
    }
  }

  document.addEventListener("pointerdown", onTrigger, true);
  document.addEventListener("click", onTrigger, true);
  document.addEventListener("keydown", onTrigger, true);

  setInterval(() => {
    const root = angularRoot();
    if (root && !watching) {
      watching = true;
      root.$watch(() => {
        const cart = root.activeCart;
        const read = cart && K.readLines(cart);
        return read ? K.signature(read.lines) : "";
      }, resetIfCartChanged);
    }
    const controller = window.angular ? paymentController() : null;
    if (controller && typeof controller.adjustDiscount === "function") checkFingerprint(controller);
    render();
  }, 2000);

  let badge;
  function render() {
    if (!document.body || !document.querySelector("payment-invoice-component")) {
      if (badge) badge.hidden = true;
      return;
    }
    if (!badge) {
      badge = document.createElement("div");
      badge.style.cssText =
        "position:fixed;left:12px;bottom:12px;z-index:2147483647;max-width:360px;padding:6px 10px;border-radius:6px;" +
        "font:12px/1.4 system-ui,sans-serif;color:#fff;box-shadow:0 2px 6px rgba(0,0,0,.25);pointer-events:none";
      document.body.appendChild(badge);
    }
    const problem = K.feedProblem(feedState, Date.now()) || (window.angular ? controllerProblem(paymentController()) : null) || lastIssue;
    const live = feedState && feedState.feed
      ? feedState.feed.programs.filter((p) => {
          const now = Date.now() + (feedState.offsetMs || 0);
          return Date.parse(p.startAtUtc) <= now && now < Date.parse(p.endAtUtc);
        }).length
      : 0;
    badge.hidden = false;
    badge.style.background = problem ? "#B91C1C" : pageWarning ? "#B45309" : "#15803D";
    badge.textContent = problem
      ? `KiotViet Tool: ${problem}`
      : `KiotViet Tool: ${live} chương trình đang giảm giá` + (pageWarning ? `. ${pageWarning}` : "");
  }
})();
