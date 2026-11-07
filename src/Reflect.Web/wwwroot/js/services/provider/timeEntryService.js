/* Pure AJAX wrappers for the time-entry form. Return Promises; never touch the DOM. */
window.TimeEntryService = (function ($) {
    'use strict';
    function draftNote(url, token, clientId) {
        return new Promise(function (resolve, reject) {
            $.ajax({ url: url, type: 'POST', dataType: 'json', headers: { 'X-Requested-With': 'XMLHttpRequest' },
                data: { __RequestVerificationToken: token, ClientId: clientId } })
                .done(resolve).fail(function (xhr) { reject(xhr); });
        });
    }
    return { draftNote: draftNote };
})(jQuery);
