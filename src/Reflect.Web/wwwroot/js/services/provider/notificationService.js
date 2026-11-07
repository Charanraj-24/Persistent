(function (window, $) {
    'use strict';
    window.NotificationService = {
        count: function (url) { return $.ajax({ url: url, type: 'GET', cache: false }); },
        list: function (url) { return $.ajax({ url: url, type: 'GET', cache: false }); },
        markAllRead: function (url, token) { return $.ajax({ url: url, type: 'POST', data: { __RequestVerificationToken: token } }); }
    };
})(window, jQuery);
