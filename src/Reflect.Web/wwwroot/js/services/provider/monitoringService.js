/* Pure AJAX wrappers for monitoring-time tracking. Returns Promises; never touches the DOM. */
var MonitoringService = (function ($) {
    'use strict';

    function token() {
        return $('input[name="__RequestVerificationToken"]').first().val();
    }

    function post(url, data) {
        return new Promise(function (resolve, reject) {
            $.ajax({
                url: url,
                type: 'POST',
                data: $.extend({ __RequestVerificationToken: token() }, data),
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).done(resolve).fail(function (xhr) { reject(xhr); });
        });
    }

    return {
        start: function (url, patientId) {
            return post(url, { patientId: patientId });
        },
        heartbeat: function (url, entryId, activeSeconds, close) {
            return post(url, { entryId: entryId, activeSeconds: activeSeconds, close: !!close });
        },
        /** Fire-and-forget final heartbeat for page unload. Uses a form body so the antiforgery token travels with it. */
        beacon: function (url, entryId, activeSeconds) {
            if (!navigator.sendBeacon) return false;
            var form = new FormData();
            form.append('__RequestVerificationToken', token());
            form.append('entryId', entryId);
            form.append('activeSeconds', activeSeconds);
            form.append('close', 'true');
            return navigator.sendBeacon(url, form);
        }
    };
})(jQuery);
