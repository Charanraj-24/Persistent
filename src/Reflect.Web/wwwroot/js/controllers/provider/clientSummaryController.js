/* Provider > Reports > Client summary: print button. */
window.ClientSummaryController = (function ($) {
    'use strict';
    function init() {
        $('.js-print').on('click', function () { window.print(); });
    }
    return { init: init };
})(jQuery);
