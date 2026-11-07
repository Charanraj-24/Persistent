/* Active review-time tracker shared by the client page and session prep. Counts seconds only while the page is
   visible, focused and the provider interacted in the last minute; sends heartbeats; flushes on hide. */
window.ProviderReviewTimer = (function ($) {
    'use strict';

    var IDLE_MS = 60000;

    function init(o) {
        var $timer = $('#reviewTimer');
        if (!$timer.length) return;
        var state = { key: o.trackingKey, pending: 0, total: 0, paused: false, sending: false, lastInteraction: Date.now(), lastSend: Date.now(), status: '' };

        function fmt(s) { var m = Math.floor(s / 60), r = s % 60; return m + ':' + (r < 10 ? '0' : '') + r; }
        function newKey() {
            if (window.crypto && crypto.randomUUID) return crypto.randomUUID().replace(/-/g, '');
            return String(Date.now()) + Math.random().toString(16).slice(2);
        }
        function isActive() {
            return !state.paused && document.visibilityState === 'visible' && document.hasFocus() && (Date.now() - state.lastInteraction) < IDLE_MS;
        }
        function render(active) {
            var status = state.paused ? o.labels.paused : active ? o.labels.active : o.labels.idle;
            $timer.toggleClass('is-active', active).toggleClass('is-paused', !active);
            $timer.find('.rpd-timer-clock').text(fmt(state.total));
            if (status !== state.status) { $timer.find('.rpd-timer-status').text(status); state.status = status; }
            $timer.find('.js-timer-toggle').text(state.paused ? 'Resume' : 'Pause').attr('aria-pressed', state.paused ? 'true' : 'false');
        }
        function payload(seconds) { return { clientId: o.clientId, trackingKey: state.key, activeSeconds: seconds }; }

        function send() {
            if (state.sending || state.pending <= 0) return;
            var seconds = Math.min(state.pending, 20);
            state.sending = true;
            state.lastSend = Date.now();
            ReviewTimeService.heartbeat(o.heartbeatUrl, o.token, payload(seconds)).then(function (res) {
                state.sending = false;
                if (res && res.ok) {
                    state.pending = Math.max(0, state.pending - seconds);
                    if (res.accepted < seconds) state.total = Math.max(0, state.total - (seconds - res.accepted));
                } else if (res && res.reset) {
                    state.key = newKey();
                    state.pending = 0;
                }
            }, function () { state.sending = false; });
        }

        function flush() {
            if (state.pending > 0 && ReviewTimeService.beacon(o.heartbeatUrl, o.token, payload(Math.min(state.pending, 20)))) state.pending = 0;
        }

        $(document).on('mousemove keydown scroll click touchstart wheel', function () { state.lastInteraction = Date.now(); });
        window.addEventListener('scroll', function () { state.lastInteraction = Date.now(); }, { passive: true });
        document.addEventListener('visibilitychange', function () { if (document.visibilityState === 'hidden') flush(); });
        window.addEventListener('pagehide', flush);
        $timer.find('.js-timer-toggle').on('click', function () { state.paused = !state.paused; state.lastInteraction = Date.now(); render(isActive()); });

        setInterval(function () {
            var active = isActive();
            if (active) { state.pending++; state.total++; }
            render(active);
            if (Date.now() - state.lastSend >= o.intervalSeconds * 1000) send();
        }, 1000);
        render(isActive());
    }

    return { init: init };
})(jQuery);
