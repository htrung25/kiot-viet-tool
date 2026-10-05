const KEYS = ["feedUrl", "readToken", "feed", "offsetMs", "fetchedAt", "error"];

function push() {
  chrome.storage.local.get(KEYS, (s) => {
    window.postMessage(
      {
        __kvtFeed: true,
        configured: !!(s.feedUrl && s.readToken),
        feed: s.feed || null,
        offsetMs: s.offsetMs || 0,
        fetchedAt: s.fetchedAt || 0,
        error: s.error || null,
      },
      location.origin,
    );
  });
}

window.addEventListener("message", (event) => {
  if (event.source === window && event.data && event.data.__kvtHello === true) push();
});
chrome.storage.onChanged.addListener(push);
push();
