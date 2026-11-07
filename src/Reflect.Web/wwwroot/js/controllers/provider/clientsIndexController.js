/* Provider > Clients: live feed with a refresh banner (the table is server-rendered and sortable). */
window.ClientsIndexController = (function ($) {
    'use strict';
    function init(options) {
        var count = 0;
        $(document).on('rpd:livedata', function (evt, events) {
            count += events.length;
            $('#newDataBanner').removeClass('d-none').find('.js-new-data-text')
                .text(count + (count === 1 ? ' update' : ' updates') + ' since this page loaded. Refresh to see the latest status.');
        });
        $('.js-reload-page').on('click', function () { window.location.reload(); });
        ProviderLiveFeed.start({ feedUrl: options.feedUrl });
    }
    return { init: init };
})(jQuery);
