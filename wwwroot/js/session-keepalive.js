(function () {
    var intervalMs = 15 * 60 * 1000; // 15 minutos
    setInterval(function () {
        fetch('/api/ping', { credentials: 'same-origin' })
            .catch(function () { /* silently ignore */ });
    }, intervalMs);
})();
