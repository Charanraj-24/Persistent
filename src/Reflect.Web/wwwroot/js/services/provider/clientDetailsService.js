/* Pure AJAX wrappers for the client page. Return Promises; never touch the DOM. */
window.ClientDetailsService = (function ($) {
    'use strict';

    /** GET one tab's HTML fragment. */
    function loadTab(url) {
        return new Promise(function (resolve, reject) {
            $.ajax({ url: url, type: 'GET', dataType: 'html', headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .done(resolve).fail(function (xhr) { reject(xhr); });
        });
    }

    return { loadTab: loadTab };
})(jQuery);
