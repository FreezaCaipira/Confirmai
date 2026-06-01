(function () {
    'use strict';

    // Cycles icons on elements marked with [data-cycle-icons].
    //
    // data-cycle-icons: comma-separated icon descriptors:
    //   "fa:fa-futbol"  - renders <i class="fas fa-futbol">
    //   "\uD83C\uDCCF"  - renders the emoji/text directly
    // data-cycle-interval: ms between swaps (default 5000)
    //
    // Host element must be a <span> wrapper; its innerHTML is replaced each tick
    // so both FA icons and emoji are supported uniformly.

    function renderIcon(icon) {
        if (icon.startsWith('fa:')) {
            return '<i class="fas ' + icon.slice(3) + '" aria-hidden="true"></i>';
        }
        return icon; // emoji or any safe text
    }

    function init() {
        document.querySelectorAll('[data-cycle-icons]').forEach(function (el) {
            var icons = el.getAttribute('data-cycle-icons').split(',').map(function (s) { return s.trim(); });
            if (icons.length < 2) return;
            if (el._iconCycleActive) return;

            el._iconCycleActive = true;
            var interval = parseInt(el.getAttribute('data-cycle-interval') || '5000', 10);
            var idx = 0;

            el.style.transition = 'opacity 0.25s';

            setInterval(function () {
                el.style.opacity = '0';
                setTimeout(function () {
                    idx = (idx + 1) % icons.length;
                    el.innerHTML = renderIcon(icons[idx]);
                    el.style.opacity = '1';
                }, 260);
            }, interval);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    window.ConfirmaiIconCycle = { init: init };
})();
