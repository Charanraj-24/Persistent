/* Provider > Reports > Monthly: summary charts and the clients table, in two tabs. */
window.MonthlyReportController = (function () {
    'use strict';

    var builders = {
        minutes: function (canvas, data) {
            var m = data.minutes || {};
            var labels = m.labels || [];
            ProviderCharts.create(canvas, function (p) {
                return {
                    type: 'bar',
                    data: {
                        labels: labels,
                        datasets: [
                            { type: 'line', label: '20 min: 98980', stack: 'threshold', data: labels.map(function () { return 20; }), borderColor: p.primary, borderDash: [6, 4], pointRadius: 0, borderWidth: 1.5 },
                            { label: 'Attested minutes', data: m.values || [], backgroundColor: p.xrFill, stack: 's', borderRadius: 3 },
                            { label: 'Pending minutes', data: m.pending || [], backgroundColor: p.warning, stack: 's', borderRadius: 3 }
                        ]
                    },
                    options: ProviderCharts.baseOptions(p, { scales: { x: { stacked: true, ticks: { autoSkip: false, maxRotation: 60, minRotation: 45 } }, y: { stacked: true, title: { display: true, text: 'Minutes', color: p.muted } } } })
                };
            });
        },
        codes: function (canvas, data) {
            var c = data.codes || {};
            ProviderCharts.create(canvas, function (p) {
                return {
                    type: 'bar',
                    data: { labels: c.labels || [], datasets: [{ label: 'Units', data: c.units || [], backgroundColor: p.xrFill, borderRadius: 4 }] },
                    options: ProviderCharts.baseOptions(p, {
                        plugins: { legend: { display: false }, tooltip: { callbacks: { afterTitle: function (items) { return (c.descriptions || [])[items[0].dataIndex] || ''; } } } },
                        scales: { y: { title: { display: true, text: 'Units', color: p.muted } } }
                    })
                };
            });
        },
        status: function (canvas, data) {
            var s = data.status || {};
            ProviderCharts.create(canvas, function (p) {
                var o = ProviderCharts.baseOptions(p, { cutout: '62%', plugins: { legend: { position: 'bottom' } } });
                delete o.scales;
                return { type: 'doughnut', data: { labels: s.labels || [], datasets: [{ data: s.counts || [], backgroundColor: [p.xrFill, p.info, p.mobileFill, p.warning], borderWidth: 2 }] }, options: o };
            });
        }
    };

    function init(options) {
        ProviderTabs.bindStatic(options.tabsId, builders, ProviderCharts.readJson(options.chartDataId));
    }

    return { init: init };
})();
