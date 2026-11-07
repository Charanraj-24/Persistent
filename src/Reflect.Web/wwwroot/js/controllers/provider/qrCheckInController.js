/* Provider > Live data demo > Audience check-in: shows a QR + join code, counts check-ins, counts down to expiry. */
window.QrCheckInController = (function ($) {
    'use strict';

    function qrSvg(text) {
        var qr = qrcode(0, 'M');
        qr.addData(text);
        qr.make();
        return qr.createSvgTag({ cellSize: 6, margin: 4, scalable: true });
    }

    function fmt(sec) { var m = Math.floor(sec / 60), s = sec % 60; return m + ':' + (s < 10 ? '0' : '') + s; }

    function init(o) {
        var $card = $('#qrCard');
        if (!$card.length) return;
        var poll = null, tick = null, secondsLeft = 0, lastCount = 0, clientId = null;

        function show(st) {
            if (!st || !st.active) {
                $card.find('.js-qr-setup').removeClass('d-none');
                $card.find('.js-qr-running, .js-qr-stage').addClass('d-none');
                $card.removeClass('is-live');
                window.clearInterval(poll); window.clearInterval(tick); poll = tick = null;
                return;
            }
            $card.addClass('is-live');
            $card.find('.js-qr-setup').addClass('d-none');
            $card.find('.js-qr-running, .js-qr-stage').removeClass('d-none');
            $card.find('.js-qr-code').html(qrSvg(st.url));
            var u = new URL(st.url);
            $card.find('.js-qr-host').text(u.host + '/checkin');
            $card.find('.js-qr-codetext').text(st.code);
            $card.find('.js-qr-local').toggleClass('d-none', !st.localOnly);
            update(st);
            if (!poll) poll = window.setInterval(refresh, 4000);
            if (!tick) tick = window.setInterval(function () {
                secondsLeft = Math.max(0, secondsLeft - 1);
                $card.find('.js-qr-left').text(fmt(secondsLeft));
                if (secondsLeft === 0) refresh();
            }, 1000);
        }

        function update(st) {
            secondsLeft = st.secondsLeft;
            $card.find('.js-qr-left').text(fmt(secondsLeft));
            var $c = $card.find('.js-qr-count').text(st.submissions);
            if (st.submissions > lastCount) { $c.removeClass('rpd-bump'); void $c[0].offsetWidth; $c.addClass('rpd-bump'); }
            lastCount = st.submissions;
        }

        function refresh() {
            CheckInService.status($card.data('status-url')).then(function (st) {
                if (st.active) update(st); else show(st);
            }, function (xhr) { if (xhr.status === 401) window.location.reload(); });
        }

        $card.on('click', '.js-qr-start', function () {
            var $b = $(this).prop('disabled', true);
            clientId = $('#qrClient').val();
            CheckInService.start($card.data('start-url'), o.token, clientId).then(function (st) {
                lastCount = 0;
                $card.find('.js-qr-client').attr('href', o.clientUrlTemplate.replace('__id__', clientId));
                show(st);
                MessageService.showToastrMessage('Check-in code ' + st.code + ' is live for 30 minutes.', 'success');
            }, function () { MessageService.showToastrMessage('Could not start the check-in.', 'error'); })
                .then(function () { $b.prop('disabled', false); });
        });

        $card.on('click', '.js-qr-stop', function () {
            CheckInService.stop($card.data('stop-url'), o.token).then(show);
        });

        $card.on('click', '.js-qr-full', function () {
            var el = $card.find('.js-qr-stage')[0];
            if (el.requestFullscreen) el.requestFullscreen().catch(function () { });
        });

        // Resume a code that's already live (for example after a page reload).
        CheckInService.status($card.data('status-url')).then(function (st) {
            if (st.active) {
                $card.find('.js-qr-client').attr('href', o.clientUrlTemplate.replace('__id__', st.clientId));
                show(st);
            }
        });
    }

    return { init: init };
})(jQuery);
