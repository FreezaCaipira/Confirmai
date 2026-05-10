window.ConfirmaiModal = (() => {
    let activeModal = null;
    let previousFocusedElement = null;
    let previousBodyOverflow = null;
    let previousDocumentOverflow = null;

    function getFocusableElements(modal) {
        if (!modal) {
            return [];
        }

        const selector = [
            "a[href]",
            "button:not([disabled])",
            "input:not([disabled]):not([type='hidden'])",
            "select:not([disabled])",
            "textarea:not([disabled])",
            "[tabindex]:not([tabindex='-1'])"
        ].join(",");

        return Array.from(modal.querySelectorAll(selector)).filter((element) => {
            if (!(element instanceof HTMLElement)) {
                return false;
            }

            return element.offsetParent !== null || element === document.activeElement;
        });
    }

    function trapTabKey(event) {
        if (event.key !== "Tab" || !activeModal) {
            return;
        }

        const focusable = getFocusableElements(activeModal);
        if (focusable.length === 0) {
            event.preventDefault();
            return;
        }

        const first = focusable[0];
        const last = focusable[focusable.length - 1];
        const current = document.activeElement;

        if (event.shiftKey && current === first) {
            event.preventDefault();
            last.focus();
            return;
        }

        if (!event.shiftKey && current === last) {
            event.preventDefault();
            first.focus();
        }
    }

    function focusFirstElement(modal) {
        const focusable = getFocusableElements(modal);
        const first = focusable[0] || modal;

        if (first instanceof HTMLElement) {
            first.focus();
        }
    }

    function close() {
        if (!activeModal) {
            // Defensive cleanup in case modal state was lost but scroll lock remained.
            if (previousBodyOverflow !== null || previousDocumentOverflow !== null) {
                document.body.style.overflow = previousBodyOverflow ?? "";
                document.documentElement.style.overflow = previousDocumentOverflow ?? "";
                previousBodyOverflow = null;
                previousDocumentOverflow = null;
            }
            return;
        }

        activeModal.removeEventListener("keydown", trapTabKey);
        activeModal = null;

        if (previousBodyOverflow !== null) {
            document.body.style.overflow = previousBodyOverflow;
        }

        if (previousDocumentOverflow !== null) {
            document.documentElement.style.overflow = previousDocumentOverflow;
        }

        if (previousFocusedElement instanceof HTMLElement) {
            previousFocusedElement.focus();
        }

        previousFocusedElement = null;
        previousBodyOverflow = null;
        previousDocumentOverflow = null;
    }

    function open(modalSelector) {
        const modal = document.querySelector(modalSelector);
        if (!(modal instanceof HTMLElement)) {
            return;
        }

        if (activeModal === modal) {
            return;
        }

        close();

        activeModal = modal;
        previousFocusedElement = document.activeElement instanceof HTMLElement ? document.activeElement : null;
        previousBodyOverflow = document.body.style.overflow;
        previousDocumentOverflow = document.documentElement.style.overflow;
        document.body.style.overflow = "hidden";
        document.documentElement.style.overflow = "hidden";

        activeModal.addEventListener("keydown", trapTabKey);

        const overlay = modal.closest(".modal-overlay");
        if (overlay instanceof HTMLElement) {
            overlay.scrollTop = 0;
        }

        modal.scrollIntoView({
            block: "center",
            inline: "nearest"
        });

        requestAnimationFrame(() => {
            focusFirstElement(activeModal);
        });
    }

    return {
        open,
        close
    };
})();

// Safety net: if user navigates away or tab visibility changes, release any stale scroll lock.
window.addEventListener("pagehide", () => {
    window.ConfirmaiModal?.close?.();
});

document.addEventListener("visibilitychange", () => {
    if (!document.hidden) {
        window.ConfirmaiModal?.close?.();
    }
});

