window.ConfirmaiCookieConsent = (() => {
    const storageKey = "Confirmai.cookieConsent.v1";
    const consentCookie = "Confirmai.cookieConsent";
    const analyticsCookie = "Confirmai.cookieAnalytics";

    const bannerId = "cookie-consent-banner";
    const modalId = "cookie-consent-modal";
    const analyticsToggleId = "cookie-analytics-toggle";

    function getEl(id) {
        return document.getElementById(id);
    }

    function show(id) {
        const el = getEl(id);
        if (el) {
            el.classList.remove("is-hidden");
        }
    }

    function hide(id) {
        const el = getEl(id);
        if (el) {
            el.classList.add("is-hidden");
        }
    }

    function writeConsent(analytics) {
        const state = {
            essential: true,
            analytics: !!analytics,
            updatedAtUtc: new Date().toISOString()
        };

        try {
            localStorage.setItem(storageKey, JSON.stringify(state));
        } catch {
            // Ignore storage failures.
        }

        try {
            if (typeof window.ConfirmaiSetCookie === "function") {
                window.ConfirmaiSetCookie(consentCookie, analytics ? "all" : "essential", 365);
                window.ConfirmaiSetCookie(analyticsCookie, analytics ? "1" : "0", 365);
            }
        } catch {
            // Ignore cookie failures.
        }
    }

    function loadConsent() {
        try {
            const raw = localStorage.getItem(storageKey);
            if (raw) {
                const parsed = JSON.parse(raw);
                return !!parsed.analytics;
            }
        } catch {
            // Ignore parse/storage failures.
        }

        try {
            if (typeof window.ConfirmaiGetCookie === "function") {
                const cookie = window.ConfirmaiGetCookie(consentCookie);
                if (cookie === "all") {
                    return true;
                }

                if (cookie === "essential") {
                    return false;
                }
            }
        } catch {
            // Ignore cookie failures.
        }

        return null;
    }

    function resetConsentForDevelopment() {
        const envFlag = window.ConfirmaiIsDevelopment;
        const isDevFlag = envFlag === true || envFlag === "true";
        const isLocalHost = window.location && (
            window.location.hostname === "localhost" ||
            window.location.hostname === "127.0.0.1" ||
            window.location.hostname === "::1"
        );

        if (!isDevFlag && !isLocalHost) {
            return;
        }

        try {
            localStorage.removeItem(storageKey);
        } catch {
            // Ignore storage failures.
        }

        try {
            document.cookie = `${consentCookie}=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/`;
            document.cookie = `${analyticsCookie}=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/`;
        } catch {
            // Ignore cookie failures.
        }
    }

    function syncToggle(analytics) {
        const input = getEl(analyticsToggleId);
        if (input) {
            input.checked = !!analytics;
        }
    }

    function init() {
        const consent = loadConsent();

        if (consent === null) {
            show(bannerId);
            hide(modalId);
            return;
        }

        syncToggle(consent);
        hide(bannerId);
        hide(modalId);
    }

    function openPreferences() {
        const consent = loadConsent();
        syncToggle(consent === null ? false : consent);
        show(modalId);
    }

    function closePreferences() {
        hide(modalId);
    }

    function acceptAll() {
        writeConsent(true);
        hide(modalId);
        hide(bannerId);
    }

    function rejectOptional() {
        writeConsent(false);
        hide(modalId);
        hide(bannerId);
    }

    function savePreferences() {
        const input = getEl(analyticsToggleId);
        writeConsent(!!(input && input.checked));
        hide(modalId);
        hide(bannerId);
    }

    const api = {
        init,
        openPreferences,
        closePreferences,
        acceptAll,
        rejectOptional,
        savePreferences
    };

    function autoInit() {
        try {
            api.init();
        } catch {
            // Ignore runtime errors to avoid breaking the page.
        }
    }

    // CSP-safe delegated click handler. Replaces inline `onclick` attributes on
    // cookie banner/modal buttons. Buttons opt in via [data-cookie-action="..."]
    // matching one of the public api method names.
    function bindActionDelegation() {
        document.addEventListener("click", (event) => {
            const target = event.target instanceof Element
                ? event.target.closest("[data-cookie-action]")
                : null;
            if (!target) {
                return;
            }
            const action = target.getAttribute("data-cookie-action");
            if (!action) {
                return;
            }
            const handler = api[action];
            if (typeof handler === "function") {
                event.preventDefault();
                handler();
            }
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", autoInit, { once: true });
    } else {
        setTimeout(autoInit, 0);
    }

    bindActionDelegation();

    return api;
})();


