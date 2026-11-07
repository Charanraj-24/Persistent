/* Provider area > Facility billing (Facility Admin): per-provider charts. Aggregate data only. */
window.FacilityBillingController = (function () {
    'use strict';

    var builders = {
        minutes: function (canvas, data) {
            ProviderCharts.create(canvas, function (p) {
                return {
                    type: 'bar',
                    data: {
                        labels: data.providers || [],
                        datasets: [
                            { label: 'Attested minutes', data: data.minutes || [], backgroundColor: p.xrFill, stack: 's', borderRadius: 3 },
                            { label: 'Pending minutes', data: data.pending || [], backgroundColor: p.warning, stack: 's', borderRadius: 3 }
                        ]
                    },
                    options: ProviderCharts.baseOptions(p, { scales: { x: { stacked: true }, y: { stacked: true, title: { display: true, text: 'Minutes', color: p.muted } } } })
                };
            });
        },
        codes: function (canvas, data) {
            ProviderCharts.create(canvas, function (p) {
                var colors = [p.xrFill, p.mobileFill, p.info, p.warning, p.success, p.primary];
                return {
                    type: 'bar',
                    data: {
                        labels: data.providers || [],
                        datasets: (data.codes || []).map(function (c, i) { return { label: c.code, data: c.units, backgroundColor: colors[i % colors.length], stack: 's' }; })
                    },
                    options: ProviderCharts.baseOptions(p, { scales: { x: { stacked: true }, y: { stacked: true, title: { display: true, text: 'Units', color: p.muted } } } })
                };
            });
        }
    };

    function init(options) {
        ProviderTabs.bindStatic(options.tabsId, builders, ProviderCharts.readJson(options.chartDataId));
    }

    return { init: init };
})();
