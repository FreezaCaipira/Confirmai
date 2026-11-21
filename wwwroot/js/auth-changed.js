// C46 (1.0): cross-tab auth-change propagation.
// A Blazor Server circuit pins its identity at connect time; when another tab
// logs out / signs in as a different account, a stale tab would keep acting
// with the old identity. This script stamps every loaded document with a
// server-provided auth fingerprint (window.__confirmaiAuthFp — a hash of the
// current user id, or "anon"), broadcasts it to other tabs, and reloads any
// tab whose fingerprint differs. bfcache-restored pages (voltar/avancar)
// compare their stale fingerprint with the stored one on `pageshow`.
(function () {
    "use strict";

    var fp = (typeof window.__confirmaiAuthFp === "string" && window.__confirmaiAuthFp) || "anon";
    var storageKey = "Confirmai.authFp.v1";
    var channelName = "Confirmai.auth";

    function reload() {
        try { location.reload(); } catch (e) { /* noop */ }
    }

    // Record the newest fingerprint seen by this browser. A page restored from
    // bfcache keeps its own (stale) `fp` while storage already holds the new one.
    try { localStorage.setItem(storageKey, fp); } catch (e) { /* storage may be unavailable */ }

    if (typeof BroadcastChannel === "function") {
        try {
            var bc = new BroadcastChannel(channelName);
            bc.onmessage = function (ev) {
                if (ev && ev.data && ev.data !== fp) { reload(); }
            };
            bc.postMessage(fp);
        } catch (e) { /* BroadcastChannel failures are non-fatal */ }
    }

    // Fallback for browsers without BroadcastChannel: the storage event fires
    // in *other* tabs when a newer document writes its fingerprint.
    window.addEventListener("storage", function (ev) {
        if (ev.key === storageKey && ev.newValue && ev.newValue !== fp) { reload(); }
    });

    // Back/forward restore (bfcache): the page's own fp is stale — compare
    // with the fingerprint recorded by the newest document.
    window.addEventListener("pageshow", function (ev) {
        if (!ev.persisted) return;
        try {
            var stored = localStorage.getItem(storageKey);
            if (stored && stored !== fp) { reload(); }
        } catch (e) { /* noop */ }
    });
})();
