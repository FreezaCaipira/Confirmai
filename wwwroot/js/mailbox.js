window.ConfirmaiMailbox = window.ConfirmaiMailbox || {
    scrollThreadToBottom: function (element) {
        if (!element) {
            return;
        }

        element.scrollTop = element.scrollHeight;
    }
};
