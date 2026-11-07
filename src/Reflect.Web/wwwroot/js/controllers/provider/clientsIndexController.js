/*
 * Provider > Clients: the table updates itself when new client data arrives.
 * The page is fetched again and its rows are swapped into the existing table, so search, sort order and the
 * current page are kept. Rows for clients with new data are highlighted for a few seconds.
 */
window.ClientsIndexController = (function ($) {
    'use strict';

    function init(options) {
        var pending = null, changedIds = {}, names = {};

        function refresh() {
            var ids = changedIds, who = Object.keys(names).map(function (k) { return names[k]; });
            changedIds = {}; names = {};
            $.ajax({ url: window.location.pathname + window.location.search, type: 'GET', cache: false, dataType: 'html' }).done(function (html) {
                var $doc = $('<div/>').append($.parseHTML(html, document, false));
                var $newRows = $doc.find('#clientsTable tbody > tr');
                if (!$newRows.length) return;
                var $table = $('#clientsTable');
                if ($.fn.dataTable && $.fn.dataTable.isDataTable($table)) {
                    var dt = $table.DataTable();
                    dt.clear();
                    dt.rows.add($newRows.toArray());
                    dt.draw(false); // keep search, sort and page
                } else {
                    $table.find('tbody').empty().append($newRows);
                }
                $table.find('tbody > tr').each(function () {
                    if (ids[$(this).attr('data-client-id')]) $(this).addClass('rpd-row-flash');
                });
                window.setTimeout(function () { $table.find('.rpd-row-flash').removeClass('rpd-row-flash'); }, 6000);
                var time = new Date().toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', second: '2-digit' });
                $('#newDataBanner').removeClass('d-none').find('.js-new-data-text')
                    .text('Updated ' + time + (who.length ? ': ' + who.slice(0, 3).join(', ') + (who.length > 3 ? ' and ' + (who.length - 3) + ' more' : '') : '') + '. Highlighted rows changed.');
            });
        }

        $(document).on('rpd:livedata', function (evt, events) {
            (events || []).forEach(function (e) { changedIds[e.clientId] = true; names[e.clientId] = e.name; });
            clearTimeout(pending);
            pending = setTimeout(refresh, 400);
        });
        ProviderLiveFeed.start({ feedUrl: options.feedUrl });
    }

    return { init: init };
})(jQuery);
