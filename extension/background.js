const ALARM = "kvt-refresh";

chrome.runtime.onInstalled.addListener(setup);
chrome.runtime.onStartup.addListener(setup);
chrome.alarms.onAlarm.addListener((alarm) => alarm.name === ALARM && refresh());
chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
  if (message !== "refresh") return false;
  refresh().then(() => sendResponse(true));
  return true;
});

function setup() {
  chrome.alarms.create(ALARM, { periodInMinutes: 1 });
  refresh();
}

async function refresh() {
  const { feedUrl, readToken } = await chrome.storage.local.get(["feedUrl", "readToken"]);
  if (!feedUrl || !readToken) {
    await chrome.storage.local.set({ error: "Chưa cấu hình địa chỉ máy chủ và mã đọc." });
    return badge("!");
  }
  try {
    const response = await fetch(feedUrl.replace(/\/+$/, "") + "/v1/feed", {
      headers: { Authorization: "Bearer " + readToken },
      cache: "no-store",
    });
    if (!response.ok) throw new Error(response.status === 401 ? "Mã đọc không đúng." : `Máy chủ trả lỗi ${response.status}.`);
    const feed = await response.json();
    if (!Array.isArray(feed.programs)) throw new Error("Dữ liệu giảm giá không hợp lệ.");
    const serverTime = Date.parse(response.headers.get("Date") || "");
    const offsetMs = Number.isNaN(serverTime) ? 0 : serverTime + 500 - Date.now();
    await chrome.storage.local.set({ feed, offsetMs, fetchedAt: Date.now(), error: null });
    badge("");
  } catch (error) {
    await chrome.storage.local.set({ error: error.message || String(error) });
    badge("!");
  }
}

function badge(text) {
  chrome.action.setBadgeText({ text });
  chrome.action.setBadgeBackgroundColor({ color: "#B91C1C" });
}
