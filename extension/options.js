const $ = (id) => document.getElementById(id);

async function show() {
  const s = await chrome.storage.local.get(["feedUrl", "readToken", "feed", "fetchedAt", "error"]);
  $("feedUrl").value = s.feedUrl || "";
  $("readToken").value = s.readToken || "";
  const status = $("status");
  const programs = s.feed ? s.feed.programs.length : 0;
  const when = s.fetchedAt ? new Date(s.fetchedAt).toLocaleString("vi-VN") : "chưa lần nào";
  status.className = s.error ? "bad" : "";
  status.textContent = (s.error ? `Lỗi: ${s.error}\n` : "") + `Tải gần nhất: ${when}\nSố chương trình chưa kết thúc: ${programs}`;
}

$("save").addEventListener("click", async () => {
  const feedUrl = $("feedUrl").value.trim().replace(/\/+$/, "");
  const readToken = $("readToken").value.trim();
  let origin;
  try {
    const url = new URL(feedUrl);
    if (url.protocol !== "https:") throw new Error();
    origin = url.origin;
  } catch {
    $("status").className = "bad";
    $("status").textContent = "Địa chỉ phải bắt đầu bằng https://";
    return;
  }
  const granted = await chrome.permissions.request({ origins: [origin + "/*"] });
  if (!granted) {
    $("status").className = "bad";
    $("status").textContent = "Cần cho phép tiện ích truy cập địa chỉ này.";
    return;
  }
  await chrome.storage.local.set({ feedUrl, readToken });
  await chrome.runtime.sendMessage("refresh");
  await show();
});

show();
