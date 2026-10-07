(function () {
  const K = window.KvtDiscount;
  const FINGERPRINT_KEY = "kvt-adjust-discount-fingerprint";
  const CHANGED_WARNING_MS = 24 * 60 * 60 * 1000;
  const SETTLE_MS = 300;
  const SETTLE_WITHOUT_INLINE_QR_MS = 1000; // nothing to watch: wait out KiotViet's debounced QR generation
  const SETTLE_TIMEOUT_MS = 3000;
  const INLINE_QR = "div.k-pay-checkout-qrcode img";
  const NOT_SETTLED = "Số tiền chưa kịp cập nhật. Đóng mã QR và mở lại để kiểm tra số tiền.";
  const applied = new WeakMap();
  const signatures = new WeakMap();
  const answered = new WeakMap(); // cart -> { signature, accepted } for the high-discount question
  let feedState = null;
  let lastIssue = null;
  let pageWarning = null;
  let watching = false;
  let lastWriteAt = 0;
  let qrBeforeWrite = null;
  let pending = false;

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
    regenerateQr(root, controller);
  }

  function inlineQrSrc() {
    const img = document.querySelector(INLINE_QR);
    return img ? img.getAttribute("src") : null;
  }

  // KiotViet redraws the transfer QR (inline and popup) from getPaymentVietQRCode, which it calls on tab or bank
  // changes but not when the discount changes.
  function regenerateQr(root, controller) {
    if (typeof controller.getPaymentVietQRCode !== "function") return;
    try {
      if (root.$$phase) controller.getPaymentVietQRCode(true);
      else root.$apply(() => controller.getPaymentVietQRCode(true));
    } catch (error) {
      console.error("[KiotViet Tool] Không vẽ lại được mã QR", error);
    }
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

  function applyAtCheckout(canAsk) {
    const root = angularRoot();
    const cart = root && root.activeCart;
    if (!cart) return;
    const mine = applied.get(cart) || 0;
    const decision = K.decide(cart, mine, feedState, Date.now());
    lastIssue = decision.reason || null;
    if (decision.action === "skip") return render();
    let amount = decision.amount;
    // A dialog opened on pointerdown swallows the click that follows: ask on click only.
    if (decision.confirm && !canAsk && answered.get(cart)?.signature !== decision.signature) return render();
    if (decision.confirm && !confirmHighDiscount(cart, decision)) {
      amount = 0;
      lastIssue = "Thu ngân đã bỏ qua mức giảm lớn cho hoá đơn này.";
    }
    const current = Number(cart.Discount) || 0;
    if (amount === mine && current === mine) return render();
    if (amount === 0 && mine === 0) return render();
    try {
      qrBeforeWrite = inlineQrSrc();
      writeVerified(root, cart, amount);
      lastWriteAt = Date.now();
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

  function inPaymentPanel(target) {
    return target instanceof Element && !!target.closest("payment-invoice-component");
  }

  // The element that opens the QR or completes the sale; short text only, so a container holding those words never matches.
  function checkoutButton(target) {
    if (!(target instanceof Element)) return null;
    // KiotViet's payment widget (k-pay / k-finance) opens its QR popup from an icon without text.
    const qrIcon = target.closest(".kfin-qr-btn, .kfin-print-qr");
    if (qrIcon) return qrIcon;
    const button = target.closest("button, a, .btn, [ng-click], [role=button]");
    const text = button ? button.textContent.normalize("NFC").replace(/\s+/g, " ").trim().toUpperCase() : "";
    return text.length <= 30 && (text === "THANH TOÁN" || /\bQR\b/.test(text)) ? button : null;
  }

  function safeApply(canAsk) {
    try {
      applyAtCheckout(canAsk);
    } catch (error) {
      lastIssue = error.message;
      console.error("[KiotViet Tool]", error);
      render();
    }
  }

  function block(event) {
    event.preventDefault();
    event.stopImmediatePropagation();
  }

  // The discount is in the cart and, when an inline transfer QR is shown, it has been redrawn (its generation is debounced).
  function settled() {
    const root = angularRoot();
    const cart = root && root.activeCart;
    if (cart && Math.abs((Number(cart.Discount) || 0) - (applied.get(cart) || 0)) > 0.01) return false;
    const qr = inlineQrSrc();
    if (qr === null || qrBeforeWrite === null) return Date.now() - lastWriteAt >= SETTLE_WITHOUT_INLINE_QR_MS;
    return qr !== qrBeforeWrite;
  }

  // Holds the cashier's action while KiotViet catches up with the discount just written, then replays it once.
  function holdAndReplay(event, replay) {
    if (Date.now() - lastWriteAt >= SETTLE_MS && settled()) {
      console.info("[KiotViet Tool] Không giữ cú bấm: giảm giá và mã QR đã cập nhật");
      return;
    }
    console.info("[KiotViet Tool] Giữ cú bấm", event.type, "để chờ số tiền cập nhật");
    block(event);
    pending = true;
    const started = Date.now();
    (function check() {
      const ready = Date.now() - lastWriteAt >= SETTLE_MS && settled();
      if (!ready && Date.now() - started < SETTLE_TIMEOUT_MS) return void setTimeout(check, 50);
      if (!ready) lastIssue = NOT_SETTLED;
      pending = false;
      console.info("[KiotViet Tool] Bấm lại sau", Date.now() - started, "ms, số tiền ổn định:", ready);
      try {
        replay();
      } catch (error) {
        lastIssue = error.message;
        console.error("[KiotViet Tool]", error);
      }
      render();
    })();
  }

  function onPointerDown(event) {
    if (event.__kvtReplay) return;
    const button = checkoutButton(event.target);
    if (pending && button) return block(event);
    if (button || inPaymentPanel(event.target)) safeApply(false);
  }

  function onClick(event) {
    if (event.__kvtReplay) return;
    const button = checkoutButton(event.target);
    if (pending && button) return block(event);
    if (!button && !inPaymentPanel(event.target)) return;
    if (!button) console.info("[KiotViet Tool] Cú bấm trong khung thanh toán, không phải nút QR / Thanh toán:", event.target);
    safeApply(true);
    if (!button) return;
    const target = event.target;
    holdAndReplay(event, () => {
      const replay = new MouseEvent("click", { bubbles: true, cancelable: true, view: window });
      replay.__kvtReplay = true;
      target.dispatchEvent(replay);
    });
  }

  function onKeyDown(event) {
    if (event.key !== "F9" || event.__kvtReplay) return;
    if (pending) return block(event);
    safeApply(true);
    const target = event.target instanceof EventTarget ? event.target : document.body;
    holdAndReplay(event, () => {
      const replay = new KeyboardEvent("keydown", { key: "F9", code: "F9", bubbles: true, cancelable: true });
      Object.defineProperty(replay, "keyCode", { value: 120 });
      Object.defineProperty(replay, "which", { value: 120 });
      replay.__kvtReplay = true;
      target.dispatchEvent(replay);
    });
  }

  window.addEventListener("pointerdown", onPointerDown, true);
  window.addEventListener("click", onClick, true);
  window.addEventListener("keydown", onKeyDown, true);

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
