/*
 * Records the provider's active review time on a patient record (the time RTM treatment management bills for).
 * Counts a second only while the tab is visible, the provider isn't idle, and tracking isn't paused.
 * The server caps each heartbeat at the wall-clock time since the last one, so the client can't inflate time.
 */
var MonitoringTimerController = (function ($) {
    'use strict';

    var IDLE_AFTER_MS = 2 * 60 * 1000;
    // A record glanced at for a few seconds isn't monitoring; the entry is created only after this much active time.
    var START_AFTER_ACTIVE_SECONDS = 5;
    var state = {
        entryId: null,
        serverPeriodSeconds: 0,
        unsentSeconds: 0,
        visitSeconds: 0,
        lastInteraction: Date.now(),
        paused: false,
        failed: false,
        sending: false,
        heartbeatEvery: 15
    };
    var opts;
    var $root, $time, $state, $toggle;

    function format(seconds) {
        var h = Math.floor(seconds / 3600);
        var m = Math.floor((seconds % 3600) / 60);
        var s = seconds % 60;
        var mm = h > 0 && m < 10 ? '0' + m : String(m);
        return (h > 0 ? h + ':' : '') + mm + ':' + (s < 10 ? '0' + s : s);
    }

    function isIdle() {
        return Date.now() - state.lastInteraction > IDLE_AFTER_MS;
    }

    function currentMode() {
        if (state.failed) return 'error';
        if (state.paused) return 'paused';
        if (document.hidden || isIdle()) return 'idle';
        return 'tracking';
    }

    function render() {
        var mode = currentMode();
        $root.attr('data-state', mode);
        $time.text(format(state.serverPeriodSeconds + state.unsentSeconds));
        var label = {
                tracking: 'Tracking · this visit ' + format(state.visitSeconds),
            idle: 'Paused while idle',
            paused: 'Paused',
            error: 'Not saving, retrying'
        }[mode];
        if ($state.text() !== label) $state.text(label);
    }

    function tick() {
        if (currentMode() === 'tracking') {
            state.unsentSeconds++;
            state.visitSeconds++;
            if (!state.entryId && !state.starting && state.visitSeconds >= START_AFTER_ACTIVE_SECONDS) start();
        }
        render();
    }

    function flush(close) {
        if (!state.entryId || state.sending) return;
        if (state.unsentSeconds === 0 && !close) return;
        var sending = state.unsentSeconds;
        state.sending = true;
        MonitoringService.heartbeat(opts.urls.heartbeat, state.entryId, sending, close)
            .then(function (res) {
                state.unsentSeconds = Math.max(0, state.unsentSeconds - sending);
                state.serverPeriodSeconds = res.periodSeconds;
                if (state.failed) MessageService.clearAllMessages();
                state.failed = false;
            })
            .catch(function () {
                if (!state.failed) MessageService.showToastrMessage(opts.messages.saveFailed, 'warning', 5000);
                state.failed = true;
            })
            .then(function () { state.sending = false; render(); });
    }

    function start() {
        state.starting = true;
        MonitoringService.start(opts.urls.start, opts.patientId)
            .then(function (res) {
                state.starting = false;
                state.entryId = res.entryId;
                state.serverPeriodSeconds = res.periodSeconds;
                state.heartbeatEvery = res.heartbeatIntervalSeconds || 15;
                state.failed = false;
                render();
                setInterval(function () { flush(false); }, state.heartbeatEvery * 1000);
            })
            .catch(function () {
                state.failed = true;
                render();
                setTimeout(start, 10000);
            });
        // Seconds counted before the entry existed are sent with the first heartbeat.
    }

    function markActive() {
        state.lastInteraction = Date.now();
    }

    function init(options) {
        opts = options;
        $root = $('#monitorTimer');
        $time = $('#monitorTimerTime');
        $state = $('#monitorTimerState');
        $toggle = $('#monitorTimerToggle');
        state.serverPeriodSeconds = options.periodSeconds || 0;

        var throttle = 0;
        $(document).on('mousemove keydown scroll click touchstart wheel', function () {
            var now = Date.now();
            if (now - throttle > 1000) { throttle = now; markActive(); }
        });
        $(window).on('scroll', markActive);

        document.addEventListener('visibilitychange', function () {
            if (document.hidden) flush(false); else markActive();
        });
        window.addEventListener('pagehide', function () {
            if (state.entryId) MonitoringService.beacon(opts.urls.heartbeat, state.entryId, state.unsentSeconds);
            state.unsentSeconds = 0;
        });

        $toggle.on('click', function () {
            state.paused = !state.paused;
            markActive();
            $toggle.attr('aria-pressed', state.paused ? 'true' : 'false');
            $toggle.find('i').attr('class', (state.paused ? 'ri-play-line' : 'ri-pause-line') + ' me-1');
            $toggle.find('.js-label').text(state.paused ? 'Resume' : 'Pause');
            MessageService.showToastrMessage(state.paused ? opts.messages.paused : opts.messages.resumed, 'info', 2000);
            if (state.paused) flush(false);
            render();
        });

        setInterval(tick, 1000);
        render();
    }

    return { init: init };
})(jQuery);
