// site.js — shared Blazor-callable helpers.
// Replaces `eval` calls with named functions (Ciclo 27, Fase F).

/**
 * Scrolls an element into view smoothly.
 * Replaces the `eval("document.getElementById('pix')?.scrollIntoView(...)")` call.
 * @param {string} elementId - The DOM element id to scroll to.
 */
window.ConfirmaiScrollToElement = (elementId) => {
    const el = document.getElementById(elementId);
    if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
};
