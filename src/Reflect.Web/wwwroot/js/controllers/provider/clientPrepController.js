/* Provider > Clients > Session prep: mood chart, print, and the review-time tracker (prep is billable review). */
window.ClientPrepController = (function ($) {
    'use strict';
    function init(options) {
        var data = ProviderCharts.readJson(options.chartDataId);
        var m = data.mood || {};
        ProviderCharts.create(document.getElementById('prepMoodChart'), function (p) {
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
        $('.js-print').on('click', function () { window.print(); });
        ProviderReviewTimer.init(options);
    }
    return { init: init };
})(jQuery);
