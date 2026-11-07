/* Pure AJAX wrappers for the provider dashboard. Return Promises; never touch the DOM. */
window.DashboardService = (function ($) {
    'use strict';

    function loadTab(url) {
        return new Promise(function (resolve, reject) {
            $.ajax({ url: url, type: 'GET', dataType: 'html', headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .done(resolve).fail(function (xhr) { reject(xhr); });
        });
    }

    return { loadTab: loadTab };
})(jQuery);
