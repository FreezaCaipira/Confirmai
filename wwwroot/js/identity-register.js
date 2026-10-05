// C38 F1 — feedback visual enquanto digita no cadastro/reset de senha.
// Serve so de ajuda: a validacao de verdade continua no servidor.
// Arquivo estatico (CSP script-src 'self' — nada inline).
(function () {
    'use strict';

    var pw = document.getElementById('Input_Password');
    if (!pw) { return; }

    var confirmEl = document.getElementById('Input_ConfirmPassword');
    var emailEl = document.getElementById('Input_Email');
    var reqs = document.querySelectorAll('.pwd-req[data-req]');
    var matchEl = document.getElementById('pwd-match');

    function checkRule(ruleId, value) {
        var parts = ruleId.split(':');
        switch (parts[0]) {
            case 'minLength': return value.length >= parseInt(parts[1], 10);
            case 'upper': return /[A-ZÀ-ÖØ-Þ]/.test(value);
            case 'lower': return /[a-zà-öø-ÿ]/.test(value);
            case 'digit': return /[0-9]/.test(value);
            case 'symbol': return /[^0-9A-Za-zÀ-ÖØ-öø-ÿ\s]/.test(value);
            case 'unique': return new Set(value).size >= parseInt(parts[1], 10);
            default: return true;
        }
    }

    function refreshReqs() {
        reqs.forEach(function (li) {
            li.classList.toggle('is-ok', checkRule(li.getAttribute('data-req'), pw.value));
        });
    }

    function refreshMatch() {
        if (!matchEl || !confirmEl) { return; }
        if (!confirmEl.value) { matchEl.hidden = true; return; }
        var ok = confirmEl.value === pw.value;
        matchEl.hidden = false;
        matchEl.textContent = ok
            ? matchEl.getAttribute('data-match')
            : matchEl.getAttribute('data-nomatch');
        matchEl.classList.toggle('is-ok', ok);
        matchEl.classList.toggle('is-bad', !ok);
    }

    pw.addEventListener('input', function () { refreshReqs(); refreshMatch(); });
    if (confirmEl) { confirmEl.addEventListener('input', refreshMatch); }

    // Email: formato no blur. Nao consulta existencia (evita enumeracao).
    if (emailEl) {
        emailEl.addEventListener('blur', function () {
            var ok = !emailEl.value || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(emailEl.value);
            emailEl.classList.toggle('input-invalid', !ok);
        });
    }

    // Mostrar/ocultar senha.
    document.querySelectorAll('[data-pwd-toggle]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var target = document.getElementById(btn.getAttribute('data-pwd-toggle'));
            if (!target) { return; }
            var show = target.type === 'password';
            target.type = show ? 'text' : 'password';
            btn.setAttribute('aria-pressed', show ? 'true' : 'false');
            btn.textContent = show
                ? btn.getAttribute('data-label-hide')
                : btn.getAttribute('data-label-show');
        });
    });
})();
