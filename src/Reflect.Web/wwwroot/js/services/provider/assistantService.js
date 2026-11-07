/* Pure AJAX wrapper for the clinical assistant. Returns Promises; never touches the DOM. */
window.AssistantService = (function ($) {
    'use strict';

    function call(opts) {
        return new Promise(function (resolve, reject) {
            $.ajax($.extend({ dataType: 'json', headers: { 'X-Requested-With': 'XMLHttpRequest' } }, opts))
                .done(resolve).fail(function (xhr) { reject($.extend({ status: xhr.status }, xhr.responseJSON || {})); });
        });
    }

    return {
        ask: function (url, token, clientId, question) {
            return call({ url: url, type: 'POST', timeout: 45000, data: { __RequestVerificationToken: token, ClientId: clientId || '', Question: question } });
        },
        history: function (url, clientId) {
            return call({ url: url, type: 'GET', cache: false, data: clientId ? { clientId: clientId } : {} });
        },
        clear: function (url, token, clientId) {
            return call({ url: url, type: 'POST', data: { __RequestVerificationToken: token, clientId: clientId || '' } });
        }
    };
})(jQuery);
