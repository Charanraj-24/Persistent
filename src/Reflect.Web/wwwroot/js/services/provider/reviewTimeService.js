/* Pure AJAX wrappers for review-time tracking. Return Promises; never touch the DOM. */
window.ReviewTimeService = (function ($) {
    'use strict';

    function heartbeat(url, token, payload) {
        return new Promise(function (resolve, reject) {
            $.ajax({
                url: url, type: 'POST', dataType: 'json',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                data: $.extend({ __RequestVerificationToken: token }, payload)
            }).done(resolve).fail(function (xhr) { reject(xhr); });
        });
    }

    /** Best-effort flush when the page is hidden or closed (sendBeacon survives navigation). */
    function beacon(url, token, payload) {
        if (!navigator.sendBeacon) return false;
        var fd = new FormData();
        fd.append('__RequestVerificationToken', token);
        Object.keys(payload).forEach(function (k) { fd.append(k, payload[k]); });
        return navigator.sendBeacon(url, fd);
    }

    return { heartbeat: heartbeat, beacon: beacon };
})(jQuery);
