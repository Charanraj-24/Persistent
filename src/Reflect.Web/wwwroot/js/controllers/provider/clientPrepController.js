/* Provider > Clients > Session prep: mood chart, print, the review-time tracker (prep is billable review),
   and live updates: when new data arrives for this client the brief is refreshed in place. */
window.ClientPrepController = (function ($) {
    'use strict';
    var chart = null;

    function drawChart(data) {
        var m = (data && data.mood) || {};
        if (chart && chart.destroy) chart.destroy();
        chart = ProviderCharts.create(document.getElementById('prepMoodChart'), function (p) {
            return {
                type: 'line',
                data: {
                    labels: m.labels || [],
                    datasets: [
                        { label: 'Before', data: m.pre || [], borderColor: p.info, backgroundColor: p.info, borderDash: [6, 4], pointStyle: 'triangle', pointRadius: 4, tension: 0.25 },
                        { label: 'After', data: m.post || [], borderColor: p.primary, backgroundColor: p.primary, pointRadius: 4, tension: 0.25 }
                    ]
                },
                options: ProviderCharts.baseOptions(p, { scales: { y: { min: 1, max: 10, beginAtZero: false } } })
            };
        });
    }

    function refresh(options) {
        $.ajax({ url: window.location.pathname, type: 'GET', cache: false, dataType: 'html' }).done(function (html) {
            var $doc = $('<div/>').append($.parseHTML(html, document, false));
            var $body = $doc.find('#prepBody');
            if (!$body.length) return;
            $('#prepBody').html($body.html()).addClass('rpd-prep-updated');
            window.setTimeout(function () { $('#prepBody').removeClass('rpd-prep-updated'); }, 2500);
            var json = $doc.find('#' + options.chartDataId).text();
            try { drawChart(JSON.parse(json)); } catch (e) { }
        });
    }

    function init(options) {
        drawChart(ProviderCharts.readJson(options.chartDataId));
        $('.js-print').on('click', function () { window.print(); });
        ProviderReviewTimer.init(options);
        if (options.feedUrl) {
            var pending = null;
            $(document).on('rpd:livedata', function (evt, events) {
                if (!(events || []).some(function (e) { return e.clientId === options.clientId; })) return;
                clearTimeout(pending);
                pending = setTimeout(function () { refresh(options); }, 300);
            });
            ProviderLiveFeed.start({ feedUrl: options.feedUrl });
        }
    }
    return { init: init };
})(jQuery);
