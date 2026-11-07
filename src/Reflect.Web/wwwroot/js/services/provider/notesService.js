/* Pure AJAX wrappers for client notes. Return Promises; never touch the DOM. */
window.NotesService = (function ($) {
    'use strict';
    function post(url, data) {
        return new Promise(function (resolve, reject) {
            $.ajax({ url: url, type: 'POST', dataType: 'json', data: data, headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .done(resolve).fail(function (xhr) { reject($.extend({ status: xhr.status }, xhr.responseJSON || {})); });
        });
    }
    return {
        save: function (url, token, note) { return post(url, $.extend({ __RequestVerificationToken: token }, note)); },
        remove: function (url, token, clientId, id) { return post(url, { __RequestVerificationToken: token, clientId: clientId, id: id }); },
        pin: function (url, token, clientId, id, pinned) { return post(url, { __RequestVerificationToken: token, clientId: clientId, id: id, pinned: pinned }); }
    };
})(jQuery);
