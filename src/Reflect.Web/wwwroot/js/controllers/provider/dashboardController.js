var ProviderDashboardController = (function () {
    'use strict';

    function init(options) {
        var weekly = options.weekly;
        var modules = options.modules;

        ProviderChartsHelper.register('weeklyChart', function (p, base) {
            base.scales.x.stacked = true;
            base.scales.y.stacked = true;
            base.scales.y.title = { display: true, text: 'Minutes', color: p.text };
            base.plugins.tooltip.callbacks = {
                title: function (items) { return 'Week of ' + items[0].label; },
                label: function (ctx) { return ' ' + ctx.dataset.label + ': ' + ctx.parsed.y + ' min'; },
                footer: function (items) {
                    var i = items[0].dataIndex;
                    return (weekly.xrMinutes[i] + weekly.mobileMinutes[i]) + ' min total · ' + weekly.sessions[i] + ' sessions';
                }
            };
            return {
                type: 'bar',
                data: {
                    labels: weekly.labels,
                    datasets: [
                        ProviderChartsHelper.barDataset('Reflect XR', weekly.xrMinutes, p.xr, p, 'platform'),
                        ProviderChartsHelper.barDataset('Reflect Mobile', weekly.mobileMinutes, p.mobile, p, 'platform')
                    ]
                },
                options: base
            };
        });

        ProviderChartsHelper.tableToggle('weeklyTableBtn', 'weeklyChartWrap', 'weeklyTable',
            ['Week of', 'Reflect XR (min)', 'Reflect Mobile (min)', 'Sessions'],
            weekly.labels.map(function (l, i) { return [l, weekly.xrMinutes[i], weekly.mobileMinutes[i], weekly.sessions[i]]; }));

        ProviderChartsHelper.register('moduleChart', function (p, base) {
            base.indexAxis = 'y';
            base.interaction = { mode: 'nearest', axis: 'y', intersect: false };
            base.scales.x.grid = { color: p.grid };
            base.scales.x.beginAtZero = true;
            base.scales.y.grid = { display: false };
            base.scales.y.ticks = { color: p.text, autoSkip: false };
            base.plugins.tooltip.callbacks = {
                label: function (ctx) {
                    var i = ctx.dataIndex;
                    return ' ' + modules.minutes[i] + ' min · ' + modules.sessions[i] + ' sessions (' + (modules.platforms[i] === 'xr' ? 'Reflect XR' : 'Reflect Mobile') + ')';
                }
            };
            var colors = modules.platforms.map(function (pl) { return pl === 'xr' ? p.xr : p.mobile; });
            var ds = ProviderChartsHelper.barDataset('Minutes', modules.minutes, colors, p);
            ds.borderSkipped = 'start';
            return { type: 'bar', data: { labels: modules.labels, datasets: [ds] }, options: base };
        });

        ProviderChartsHelper.tableToggle('moduleTableBtn', 'moduleChartWrap', 'moduleTable',
            ['Module', 'Platform', 'Minutes', 'Sessions'],
            modules.labels.map(function (l, i) { return [l, modules.platforms[i] === 'xr' ? 'Reflect XR' : 'Reflect Mobile', modules.minutes[i], modules.sessions[i]]; }));
    }

    return { init: init };
})();
