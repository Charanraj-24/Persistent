/* Pure AJAX wrappers for the live demo page. Return Promises; never touch the DOM. */
window.LiveDemoService = (function ($) {
    'use strict';

    function runScenario(url, token, clientId, scenario) {
        return new Promise(function (resolve, reject) {
            $.ajax({
                url: url, type: 'POST', dataType: 'json', headers: { 'X-Requested-With': 'XMLHttpRequest' },
                data: { __RequestVerificationToken: token, ClientId: clientId, Scenario: scenario }
            }).done(resolve).fail(function (xhr) { reject(xhr.responseJSON || {}); });
        });
    }

    return { runScenario: runScenario };
})(jQuery);
