/* Shared behavior for Provider Dashboard create/edit forms: US-format date and time pickers, goal templates. */
var ProviderFormController = (function ($) {
    'use strict';

    function initPickers() {
        $('.js-date').each(function () {
            var $el = $(this);
            flatpickr(this, {
                disableMobile: true,
                dateFormat: 'Y-m-d',
                altInput: true,
                altFormat: 'n/j/Y',
                allowInput: false,
                minDate: $el.data('min-date') || null,
                maxDate: $el.data('max-date') || null
            });
        });
        $('.js-time').each(function () {
            flatpickr(this, {
                disableMobile: true,
                enableTime: true,
                noCalendar: true,
                dateFormat: 'H:i',
                altInput: true,
                altFormat: 'h:i K',
                time_24hr: false
            });
        });
    }

    function initGoalTemplates() {
        $(document).on('click', '.js-goal-template', function () {
            var $b = $(this);
            $('#Title').val($b.data('title')).trigger('change');
            $('#MeasureName').val($b.data('measure'));
            $('#Unit').val($b.data('unit'));
            $('#Direction').val(String($b.data('direction')));
            $('#Title').trigger('focus');
        });
    }

    function init() {
        initPickers();
        initGoalTemplates();
        $('form.js-loading-submit').on('submit', function () {
            if ($(this).valid && !$(this).valid()) return;
            window.displayLoading('Saving...');
        });
    }

    return { init: init };
})(jQuery);
