var PatientDetailsController = (function ($) {
    'use strict';

    var H = ProviderChartsHelper;
    var rendered = {};

    function weekly(data) {
        H.register('weeklyChart', function (p, base) {
            base.scales.x.stacked = true;
            base.scales.y.stacked = true;
            base.plugins.tooltip.callbacks = {
                title: function (items) { return 'Week of ' + items[0].label; },
                label: function (ctx) { return ' ' + ctx.dataset.label + ': ' + ctx.parsed.y + ' min'; },
                footer: function (items) { return data.sessions[items[0].dataIndex] + ' sessions'; }
            };
            return {
                type: 'bar',
                data: {
                    labels: data.labels,
                    datasets: [
                        H.barDataset('Reflect XR', data.xrMinutes, p.xr, p, 'platform'),
                        H.barDataset('Reflect Mobile', data.mobileMinutes, p.mobile, p, 'platform')
                    ]
                },
                options: base
            };
        });
        H.tableToggle('weeklyTableBtn', 'weeklyChartWrap', 'weeklyTable', ['Week of', 'Reflect XR (min)', 'Reflect Mobile (min)', 'Sessions'],
            data.labels.map(function (l, i) { return [l, data.xrMinutes[i], data.mobileMinutes[i], data.sessions[i]]; }));
    }

    function modules(data) {
        if (!data.labels.length) return;
        H.register('moduleChart', function (p, base) {
            base.indexAxis = 'y';
            base.interaction = { mode: 'nearest', axis: 'y', intersect: false };
            base.scales.x.grid = { color: p.grid };
            base.scales.y.grid = { display: false };
            base.scales.y.ticks = { color: p.text, autoSkip: false };
            base.plugins.tooltip.callbacks = {
                label: function (ctx) { return ' ' + data.minutes[ctx.dataIndex] + ' min · ' + data.sessions[ctx.dataIndex] + ' sessions'; }
            };
            var colors = data.platforms.map(function (pl) { return pl === 'xr' ? p.xr : p.mobile; });
            return { type: 'bar', data: { labels: data.labels, datasets: [H.barDataset('Minutes', data.minutes, colors, p)] }, options: base };
        });
        H.tableToggle('moduleTableBtn', 'moduleChartWrap', 'moduleTable', ['Module', 'Platform', 'Minutes', 'Sessions'],
            data.labels.map(function (l, i) { return [l, data.platforms[i] === 'xr' ? 'Reflect XR' : 'Reflect Mobile', data.minutes[i], data.sessions[i]]; }));
    }

    function mood(data) {
        if (!data.labels.length) return;
        H.register('moodChart', function (p, base) {
            base.scales.y.min = 0;
            base.scales.y.max = 10;
            base.scales.y.beginAtZero = true;
            base.plugins.tooltip.callbacks = {
                label: function (ctx) { return ' Mood ' + ctx.dataset.label.toLowerCase() + ': ' + ctx.parsed.y; },
                footer: function (items) {
                    var i = items[0].dataIndex;
                    var d = data.after[i] - data.before[i];
                    return 'Change: ' + (d > 0 ? '+' : '') + d;
                }
            };
            return {
                type: 'line',
                data: {
                    labels: data.labels,
                    datasets: [H.lineDataset('Before', data.before, p.xr, p), H.lineDataset('After', data.after, p.mobile, p)]
                },
                options: base
            };
        });
        H.tableToggle('moodTableBtn', 'moodChartWrap', 'moodTable', ['Session', 'Before', 'After', 'Change'],
            data.labels.map(function (l, i) { var d = data.after[i] - data.before[i]; return [l, data.before[i], data.after[i], (d > 0 ? '+' : '') + d]; }));
    }

    function assessments(data) {
        if (!data.labels.length) return;
        H.register('assessmentChart', function (p, base) {
            base.scales.y.suggestedMax = 27;
            base.plugins.tooltip.callbacks = {
                label: function (ctx) { return ctx.parsed.y === null ? null : ' ' + ctx.dataset.label + ': ' + ctx.parsed.y; }
            };
            return {
                type: 'line',
                data: {
                    labels: data.labels,
                    datasets: [H.lineDataset('PHQ-9', data.phq9, p.xr, p, true), H.lineDataset('GAD-7', data.gad7, p.mobile, p)]
                },
                options: base
            };
        });
        H.tableToggle('assessmentTableBtn', 'assessmentChartWrap', 'assessmentTable', ['Date', 'PHQ-9', 'GAD-7'],
            data.labels.map(function (l, i) { return [l, data.phq9[i], data.gad7[i]]; }));
    }

    function physiologic(data) {
        if (!data.hasData) return;
        function pair(canvasId, label, start, end, unit) {
            H.register(canvasId, function (p, base) {
                base.scales.y.beginAtZero = false;
                base.plugins.tooltip.callbacks = {
                    label: function (ctx) { return ' ' + ctx.dataset.label + ': ' + ctx.parsed.y + ' ' + unit; }
                };
                return {
                    type: 'line',
                    data: {
                        labels: data.labels,
                        datasets: [H.lineDataset('Start', start, p.mobile, p), H.lineDataset('End', end, p.xr, p)]
                    },
                    options: base
                };
            });
        }
        pair('hrChart', 'Heart rate', data.heartRateStart, data.heartRateEnd, 'bpm');
        pair('hrvChart', 'HRV', data.hrvStart, data.hrvEnd, 'ms');
        H.tableToggle('hrTableBtn', 'hrChartWrap', 'hrTable', ['Session', 'Start (bpm)', 'End (bpm)'],
            data.labels.map(function (l, i) { return [l, data.heartRateStart[i], data.heartRateEnd[i]]; }));
        H.tableToggle('hrvTableBtn', 'hrvChartWrap', 'hrvTable', ['Session', 'Start (ms)', 'End (ms)'],
            data.labels.map(function (l, i) { return [l, data.hrvStart[i], data.hrvEnd[i]]; }));
    }

    // Charts in hidden tabs measure zero width, so each tab's charts are drawn the first time it is shown.
    function renderTab(tab, charts) {
        if (rendered[tab]) return;
        rendered[tab] = true;
        if (tab === 'overview') { weekly(charts.weekly); modules(charts.modules); mood(charts.mood); }
        if (tab === 'goals') assessments(charts.assessments);
        if (tab === 'physiologic') physiologic(charts.physiologic);
    }

    function init(options) {
        initTabDataTableFix('#patientTabs');

        var $active = $('#patientTabs .nav-link.active');
        renderTab($active.data('tab'), options.charts);

        $('#patientTabs').on('shown.bs.tab', '[data-bs-toggle="tab"]', function (e) {
            var tab = $(e.target).data('tab');
            renderTab(tab, options.charts);
            try {
                var url = new URL(window.location.href);
                if (tab === 'overview') url.searchParams.delete('tab'); else url.searchParams.set('tab', tab);
                window.history.replaceState(null, '', url.toString());
            } catch (err) { /* ignore */ }
        });

        if (options.trackingEnabled) {
            MonitoringTimerController.init(options);
        }
    }

    return { init: init };
})(jQuery);
