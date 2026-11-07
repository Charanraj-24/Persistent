/* Pure AJAX wrappers for the QR audience check-in. Return Promises; never touch the DOM. */
window.CheckInService = (function ($) {
    'use strict';
    function call(url, type, data) {
        return new Promise(function (resolve, reject) {
            $.ajax({ url: url, type: type, cache: false, dataType: 'json', data: data, headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .done(resolve).fail(function (xhr) { reject(xhr); });
        });
    }
    return {
        start: function (url, token, clientId) { return call(url, 'POST', { __RequestVerificationToken: token, clientId: clientId }); },
        stop: function (url, token) { return call(url, 'POST', { __RequestVerificationToken: token }); },
        status: function (url) { return call(url, 'GET'); }
    };
})(jQuery);
