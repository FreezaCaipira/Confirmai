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

