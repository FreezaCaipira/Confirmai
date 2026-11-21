// C46 Fase 3: Web Share API wrapper para o modal de convite.
// navigator.share requer user gesture + contexto seguro; quando indisponivel
// o chamador cai no fluxo de copiar link.
window.confirmaiShare = {
    canShare: function () {
        return typeof navigator !== "undefined" && typeof navigator.share === "function";
    },
    share: function (title, text, url) {
        if (!window.confirmaiShare.canShare()) {
            return Promise.resolve(false);
        }
        return navigator.share({ title: title, text: text, url: url })
            .then(function () { return true; })
            .catch(function () { return false; }); // AbortError (usuario fechou) nao e erro
    }
};
