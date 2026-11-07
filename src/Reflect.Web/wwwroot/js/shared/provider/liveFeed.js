/* Provider dashboard live feed: polls /Provider/Live/Feed for new client data, shows toasts, updates the
   "Live" pill, and fires a jQuery 'rpd:livedata' event on document so each screen can refresh what it shows.
   Polling pauses while the browser tab is hidden. Same-origin only (CSP-safe). */
window.ProviderLiveFeed = (function ($) {
    'use strict';

    var started = false;

    function clock() {
        return new Date().toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', second: '2-digit' });
    }

    function setPill(text, ok) {
        var $p = $('#liveIndicator');
        if (!$p.length) return;
        $p.toggleClass('is-offline', !ok).find('.js-live-text').text(text);
    }

    function toast(events, quiet) {
        var urgent = events.filter(function (e) { return e.isUrgent; });
        urgent.forEach(function (e) { MessageService.showToastrMessage('Urgent: ' + e.name + ' - ' + e.summary, 'error', 12000); });
        if (quiet) return;
        var normal = events.filter(function (e) { return !e.isUrgent; });
        normal.slice(0, 2).forEach(function (e) { MessageService.showToastrMessage(e.name + ': ' + e.summary, 'info', 4000); });
        if (normal.length > 2) MessageService.showToastrMessage((normal.length - 2) + ' more updates', 'info', 4000);
    }

    /** options: feedUrl, intervalMs (default 5000), quiet (only urgent toasts) */
    function start(options) {
        if (started) return;
        started = true;
        var after = null, timer = null;
        var interval = options.intervalMs || 5000;

        function schedule() { clearTimeout(timer); timer = setTimeout(poll, interval); }

        function poll() {
            if (document.hidden) { schedule(); return; }
            $.ajax({ url: options.feedUrl, type: 'GET', dataType: 'json', data: after === null ? {} : { after: after }, headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .done(function (r) {
                    after = r.now;
                    var events = r.events || [];
                    if (events.length) {
                        toast(events, options.quiet);
                        $(document).trigger('rpd:livedata', [events]);
                    }
                    setPill('Live, updated ' + clock(), true);
                })
                .fail(function (xhr) {
                    if (xhr.status === 401) { window.location.href = '/StandinAccount/Login?timedOut=true&returnUrl=' + encodeURIComponent(window.location.pathname); return; }
                    setPill('Offline, retrying', false);
                })
                .always(schedule);
        }

        document.addEventListener('visibilitychange', function () { if (!document.hidden) poll(); });
        // A page restored with Back/Forward may be out of date: reload it.
        window.addEventListener('pageshow', function (e) { if (e.persisted) window.location.reload(); });
        poll();
    }

    return { start: start };
})(jQuery);
