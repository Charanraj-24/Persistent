/* Provider > Time entries > Create: show the communication-mode field only for live interactions. */
window.TimeEntryCreateController = (function ($) {
    'use strict';

    function init(options) {
        var $activity = $('#' + options.activityId);
        var $modeGroup = $('#' + options.modeGroupId);
        var $mode = $modeGroup.find('select');

        function sync() {
            var live = String($activity.val()) === String(options.liveValue);
            $modeGroup.toggleClass('d-none', !live);
            if (!live) $mode.val(options.noneValue);
            else if (String($mode.val()) === String(options.noneValue)) $mode.val(options.defaultLiveMode);
        }
        $activity.on('change', sync);
        sync();

        // Draft the documentation note from the client's data; the provider reviews and edits before saving.
        var $note = $('#Note'), $status = $('.js-draft-status');
        $('.js-draft-note').on('click', function () {
            var $btn = $(this);
            $btn.prop('disabled', true);
            $status.text('Drafting from this month\'s data...');
            TimeEntryService.draftNote($btn.data('url'), options.token, options.clientId)
                .then(function (r) {
                    $note.val(r.note).trigger('input');
                    $status.text(r.ai ? 'Drafted with AI from de-identified data. Review and edit before you attest.' : 'Drafted from the data. Review and edit before you attest.');
                }, function () { $status.text('The draft could not be created. Write the note yourself.'); })
                .then(function () { $btn.prop('disabled', false); });
        });
    }

    return { init: init };
})(jQuery);
