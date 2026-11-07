/* Provider > Live data demo: run scenarios and show incoming events as they arrive. */
window.LiveDemoController = (function ($) {
    'use strict';

    function row(e, clientUrlTemplate) {
        var $li = $('<li/>').toggleClass('rpd-live-urgent', !!e.isUrgent).addClass('rpd-live-new');
        $li.append($('<span class="avatar avatar-sm avatar-rounded"/>').text(e.initials));
        var $body = $('<div class="flex-grow-1 min-w-0"/>');
        var $top = $('<div class="d-flex flex-wrap justify-content-between gap-1"/>');
        $top.append($('<a class="rpd-name"/>').attr('href', clientUrlTemplate.replace('__id__', e.clientId)).text(e.name));
        $top.append($('<span class="text-muted fs-12"/>').text(e.when + ' · ' + e.source));
        var $sum = $('<div class="fs-13"/>');
        if (e.isUrgent) {
            $sum.append($('<i class="ri-alarm-warning-line text-danger me-1" aria-hidden="true"/>'));
            $sum.append($('<span class="visually-hidden"/>').text('Urgent: '));
        }
        $sum.append(document.createTextNode(e.summary));
        $body.append($top, $sum);
        return $li.append($body);
    }

    function init(o) {
        ProviderLiveFeed.start({ feedUrl: o.feedUrl, quiet: true });

        $(document).on('rpd:livedata', function (evt, events) {
            var $log = $('#liveLog');
            events.forEach(function (e) { $log.prepend(row(e, o.clientUrlTemplate)); });
            $log.children().slice(40).remove();
            $('#liveEmpty').addClass('d-none');
        });

        $('.js-run-scenario').on('click', function () {
            var $btn = $(this);
            var scenario = $btn.data('scenario');
            var run = function () {
                $btn.prop('disabled', true);
                LiveDemoService.runScenario(o.runUrl, o.token, $('#liveClient').val(), scenario)
                    .then(function (r) { MessageService.showToastrMessage('Sent: ' + r.summary, 'success'); },
                        function (err) { MessageService.showToastrMessage(err.error || 'The event could not be sent.', 'error'); })
                    .then(function () { $btn.prop('disabled', false); });
            };
            if ($btn.hasClass('is-safety')) {
                MessageService.showConfirmationDialogInfo('Send a safety event?',
                    'This simulates a PHQ-9 with item 9 endorsed and raises an urgent flag for the selected client.',
                    'Send', 'Cancel', run, MessageService.cancelAction);
            } else { run(); }
        });
    }

    return { init: init };
})(jQuery);
