(function () {
    'use strict';

    // Create the progress bar element dynamically — no HTML changes required
    var bar = document.createElement('div');
    bar.id = 'nav-progress-bar';
    bar.style.cssText = [
        'position:fixed', 'top:0', 'left:0', 'width:0%', 'height:3px',
        'z-index:99999', 'opacity:0', 'pointer-events:none',
        'border-radius:0 2px 2px 0',
        'background:linear-gradient(90deg,#4f9cf8,#a5d0ff)',
        'box-shadow:0 0 10px rgba(79,156,248,0.7)',
        'transition:none'
    ].join(';');

    document.addEventListener('DOMContentLoaded', function () {
        document.body.appendChild(bar);
    });

    var active = false;
    var raf    = null;
    var tid    = null;
    var prog   = 0;

    function start() {
        if (active) return;
        active = true;
        prog = 0;
        bar.style.transition = 'none';
        bar.style.opacity    = '1';
        bar.style.width      = '0%';
        if (raf) cancelAnimationFrame(raf);
        if (tid) clearTimeout(tid);
        step();
        // Safety: auto-finish after 10 s if Blazor never responds
        tid = setTimeout(finish, 10000);
    }

    function step() {
        if (!active) return;
        // Fast at start, asymptotically slow near 88 %
        var speed   = prog < 40 ? 1.2 : prog < 70 ? 0.5 : prog < 85 ? 0.15 : 0.02;
        var ceiling = prog < 40 ? 40  : prog < 70 ? 70  : 88;
        prog = Math.min(prog + speed, ceiling);
        bar.style.width = prog + '%';
        raf = requestAnimationFrame(step);
    }

    function finish() {
        if (!active) return;
        active = false;
        if (raf) { cancelAnimationFrame(raf); raf = null; }
        if (tid) { clearTimeout(tid);         tid = null; }
        bar.style.transition = 'width 0.25s ease-out';
        bar.style.width      = '100%';
        setTimeout(function () {
            bar.style.transition = 'opacity 0.35s ease';
            bar.style.opacity    = '0';
            setTimeout(function () {
                bar.style.transition = 'none';
                bar.style.width      = '0%';
            }, 400);
        }, 150);
    }

    // Start the bar on every internal link click
    document.addEventListener('click', function (e) {
        var a = e.target.closest('a[href]');
        if (!a || a.target === '_blank') return;

        var href = a.getAttribute('href');
        if (!href) return;
        if (href.charAt(0) === '#') return;
        if (/^(mailto:|tel:|javascript:|blob:)/i.test(href)) return;

        // Skip external links (absolute URLs pointing elsewhere)
        if (/^https?:\/\//i.test(href) && href.indexOf(window.location.origin) !== 0) return;

        // Skip same-page navigation
        try {
            var target = new URL(href, window.location.href);
            if (target.pathname === window.location.pathname &&
                target.search   === window.location.search) return;
        } catch (ex) { return; }

        start();
    });

    // Exposed so Blazor's MainLayout can call finish() after each render
    window.navProgress = { start: start, finish: finish };
}());
