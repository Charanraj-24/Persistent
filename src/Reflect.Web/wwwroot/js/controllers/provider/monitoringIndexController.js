var MonitoringIndexController = (function () {
    'use strict';

    function init(data) {
        ProviderChartsHelper.register('dailyChart', function (p, base) {
            base.plugins.tooltip.callbacks = {
                label: function (ctx) { return ' ' + ctx.parsed.y + ' min'; }
            };
            return {
                type: 'bar',
                data: { labels: data.labels, datasets: [ProviderChartsHelper.barDataset('Minutes', data.minutes, p.xr, p)] },
                options: base
            };
        });
        ProviderChartsHelper.tableToggle('dailyTableBtn', 'dailyChartWrap', 'dailyTable', ['Date', 'Minutes'],
            data.labels.map(function (l, i) { return [l, data.minutes[i]]; }));
    }

    return { init: init };
})();
