/* Provider > Dashboard: lazy-loaded tabs and their charts. */
window.ProviderDashboardController = (function ($) {
    'use strict';

    function noAxes(o) { delete o.scales; return o; }

    var builders = {
        activity: function (canvas, data) {
            var a = data.activity || {};
            ProviderCharts.create(canvas, function (p) {
                return {
                    type: 'bar',
                    data: {
                        labels: a.labels || [],
                        datasets: [
                            { label: 'Reflect XR sessions', data: a.xr || [], backgroundColor: p.xrFill, borderColor: p.xr, borderWidth: 1, stack: 's' },
                            { label: 'Reflect Mobile sessions', data: a.mobile || [], backgroundColor: p.mobileFill, borderColor: p.mobile, borderWidth: 1, stack: 's' }
                        ]
                    },
                    options: ProviderCharts.baseOptions(p, { scales: { x: { stacked: true }, y: { stacked: true, title: { display: true, text: 'Sessions', color: p.muted } } } })
                };
            });
        },
        experiences: function (canvas, data) {
            var exp = data.experiences || [];
            ProviderCharts.create(canvas, function (p) {
                return {
                    type: 'bar',
                    data: { labels: exp.map(function (e) { return e.label; }), datasets: [{ label: 'Minutes (last 30 days)', data: exp.map(function (e) { return e.minutes; }), backgroundColor: p.xrFill, borderRadius: 4 }] },
                    options: ProviderCharts.baseOptions(p, { indexAxis: 'y', plugins: { legend: { display: false } }, scales: { x: { grid: { display: true } }, y: { grid: { display: false } } } })
                };
            });
        },
        tiers: function (canvas, data) {
            var t = data.tiers || {};
            ProviderCharts.create(canvas, function (p) {
                return {
                    type: 'bar',
                    data: { labels: t.labels || [], datasets: [{ label: 'Clients', data: t.counts || [], backgroundColor: [p.mobileFill, p.info, p.xrFill], borderRadius: 4 }] },
                    options: ProviderCharts.baseOptions(p, { plugins: { legend: { display: false } }, scales: { y: { title: { display: true, text: 'Clients', color: p.muted } } } })
                };
            });
        },
        beforeAfter: function (canvas, data) {
            var d = data.beforeAfter || {};
            ProviderCharts.create(canvas, function (p) {
                return {
                    type: 'bar',
                    data: { labels: d.labels || [], datasets: [
                        { label: 'Average at baseline', data: d.baseline || [], backgroundColor: p.info, borderRadius: 4 },
                        { label: 'Average now', data: d.latest || [], backgroundColor: p.xrFill, borderRadius: 4 }
                    ] },
                    options: ProviderCharts.baseOptions(p, { scales: { y: { title: { display: true, text: 'Score (lower is better)', color: p.muted } } } })
                };
            });
        },
        scatter: function (canvas, data) {
            var pts = data.scatter || [];
            ProviderCharts.create(canvas, function (p) {
                return {
                    type: 'scatter',
                    data: { datasets: [{ label: 'Clients', data: pts, backgroundColor: p.mobileFill, borderColor: p.mobile, pointRadius: 6, pointHoverRadius: 8 }] },
                    options: ProviderCharts.baseOptions(p, {
                        interaction: { mode: 'nearest', intersect: true },
                        plugins: { legend: { display: false }, tooltip: { callbacks: { label: function (c) { return c.raw.name + ': ' + c.raw.x + ' active days, PHQ-9 ' + (c.raw.y > 0 ? '+' : '') + c.raw.y; } } } },
                        scales: {
                            x: { type: 'linear', min: 0, max: 30, title: { display: true, text: 'Active days, last 30', color: p.muted }, grid: { display: true } },
                            y: { beginAtZero: false, title: { display: true, text: 'PHQ-9 change since baseline', color: p.muted } }
                        }
                    })
                };
            });
        },
        status: function (canvas, data) {
            var s = data.status || {};
            ProviderCharts.create(canvas, function (p) {
                return {
                    type: 'doughnut',
                    data: { labels: s.labels || [], datasets: [{ data: s.counts || [], backgroundColor: [p.xrFill, p.info, p.mobileFill], borderWidth: 2 }] },
                    options: noAxes(ProviderCharts.baseOptions(p, { cutout: '62%', plugins: { legend: { position: 'bottom' } } }))
                };
            });
        }
    };

    function init(options) {
        var tabs = ProviderTabs.init({
            tabsId: options.tabsId, load: DashboardService.loadTab, builders: builders, loadFailed: options.loadFailed,
            onLoaded: function (pane) { if (pane.id === 'dtab-usage' && window.ModuleUsageController) ModuleUsageController.mount($(pane)); }
        });

        // Live data: refresh the KPI tiles and the open tab when new client data arrives.
        var pending = null;
        $(document).on('rpd:livedata', function () {
            clearTimeout(pending);
            pending = setTimeout(function () {
                var $k = $('#dashboardKpis');
                DashboardService.loadTab($k.attr('data-refresh-url')).then(function (html) { $k.html(html); }, function () { });
                var $usage = $('#dtab-usage');
                if ($usage.hasClass('active') && $usage.data('state') === 'loaded' && window.ModuleUsageController) {
                    // Update the usage explorer in place so filters, selection and typing are kept.
                    $('#' + options.tabsId + 'Content > .tab-pane').not($usage).data('state', '');
                    ModuleUsageController.refresh($usage.attr('data-tab-url'));
                } else {
                    tabs.reloadActive();
                }
            }, 300);
        });
        ProviderLiveFeed.start({ feedUrl: options.feedUrl });
    }

    return { init: init };
})(jQuery);
