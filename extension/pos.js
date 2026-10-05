(function () {
  const K = window.KvtDiscount;
  const applied = new WeakMap();
  const signatures = new WeakMap();
  let feedState = null;
  let lastIssue = null;
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

  function writeDiscount(root, cart, amount) {
    const controller = paymentController();
    if (!controller) throw new Error("Không tìm thấy ô Giảm giá của KiotViet (có thể KiotViet vừa cập nhật giao diện).");
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

  function applyAtCheckout() {
    const root = angularRoot();
    const cart = root && root.activeCart;
    if (!cart) return;
    const mine = applied.get(cart) || 0;
    const decision = K.decide(cart, mine, feedState, Date.now());
    lastIssue = decision.reason || null;
    if (decision.action === "skip") return render();
    const current = Number(cart.Discount) || 0;
    if (decision.amount === mine && current === mine) return render();
    if (decision.amount === 0 && mine === 0) return render();
    try {
      writeDiscount(root, cart, decision.amount);
      applied.set(cart, decision.amount);
      signatures.set(cart, decision.signature);
      console.info("[KiotViet Tool] Giảm giá hoá đơn:", decision.amount);
    } catch (error) {
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
    const problem = K.feedProblem(feedState, Date.now()) || (window.angular && !paymentController() ? "Không tìm thấy ô Giảm giá của KiotViet." : null) || lastIssue;
    const live = feedState && feedState.feed
      ? feedState.feed.programs.filter((p) => {
          const now = Date.now() + (feedState.offsetMs || 0);
          return Date.parse(p.startAtUtc) <= now && now < Date.parse(p.endAtUtc);
        }).length
      : 0;
    badge.hidden = false;
    badge.style.background = problem ? "#B91C1C" : "#15803D";
    badge.textContent = problem ? `KiotViet Tool: ${problem}` : `KiotViet Tool: ${live} chương trình đang giảm giá`;
  }
})();
