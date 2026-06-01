window.ConfirmaiSetCookie = (name, value, days) => {
    const maxAgeDays = Number.isFinite(days) ? days : 365;
    const safeName = encodeURIComponent(name);
    const safeValue = encodeURIComponent(value ?? "");
    const maxAge = Math.max(1, Math.floor(maxAgeDays * 24 * 60 * 60));
    document.cookie = `${safeName}=${safeValue}; path=/; max-age=${maxAge}; samesite=lax`;
};

window.ConfirmaiGetCookie = (name) => {
    const safeName = encodeURIComponent(name) + "=";
    const parts = (document.cookie || "").split(";");
    for (const rawPart of parts) {
        const part = rawPart.trim();
        if (part.startsWith(safeName)) {
            const value = part.substring(safeName.length);
            try {
                return decodeURIComponent(value);
            } catch {
                return value;
            }
        }
    }
    return null;
};

window.ConfirmaiIsDocumentVisible = () => {
    if (typeof document === "undefined") {
        return true;
    }

    return document.visibilityState !== "hidden";
};

/**
 * Detects whether a Google Maps iframe loaded successfully (cross-origin from Google)
 * or was blocked by the browser (stays at about:blank / same-origin).
 * - Cross-origin  → Google responded → show the map, hide the fallback.
 * - Same-origin   → browser blocked  → keep the fallback, hide the map wrap.
 */
window.ConfirmaiSetupMapFallback = (mapWrapId, fallbackId) => {
    const wrap     = document.getElementById(mapWrapId);
    const fallback = document.getElementById(fallbackId);
    if (!wrap || !fallback) return;

    const iframe = wrap.querySelector('iframe');
    if (!iframe) return;

    const showFallback = () => {
        wrap.style.display     = 'none';
        fallback.style.display = '';
    };
    const showMap = () => {
        fallback.style.display = 'none';
        wrap.style.display     = '';
    };

    // Safety timeout – if load never fires, fall back after 8 s
    const timer = setTimeout(showFallback, 8000);

    iframe.addEventListener('load', () => {
        clearTimeout(timer);
        try {
            // For cross-origin iframes (Google domain), contentDocument is null.
            // For same-origin iframes (about:blank, blocked by extension), it is accessible.
            const isBlocked = iframe.contentDocument !== null;
            if (isBlocked) showFallback(); else showMap();
        } catch {
            // SecurityError means cross-origin → Google responded
            showMap();
        }
    });
};

