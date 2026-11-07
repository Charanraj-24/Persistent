/* Provider > Clients > Details: lazy-loaded tabs, their charts, and the active review-time tracker. */
window.ClientDetailsController = (function ($) {
    'use strict';


    function bar(p, labels, datasets, extra) {
        return { type: 'bar', data: { labels: labels || [], datasets: datasets }, options: ProviderCharts.baseOptions(p, extra || {}) };
    }

    function prePost(p, labels, pre, post, preLabel, postLabel, yTitle, yOpts) {
        return {
            type: 'line',
            data: {
                labels: labels || [],
                datasets: [
                    { label: preLabel, data: pre || [], borderColor: p.info, backgroundColor: p.info, borderDash: [6, 4], pointStyle: 'triangle', pointRadius: 4, tension: 0.25 },
                    { label: postLabel, data: post || [], borderColor: p.primary, backgroundColor: p.primary, pointStyle: 'circle', pointRadius: 4, tension: 0.25 }
                ]
            },
            options: ProviderCharts.baseOptions(p, { scales: { y: $.extend({ title: { display: true, text: yTitle, color: p.muted } }, yOpts || {}) } })
        };
    }

    /** Chart builders by data-chart name. Each receives the canvas and its tab's JSON. */
    var builders = {
        dailyMinutes: function (canvas, data) {
            var d = data.daily || {};
            ProviderCharts.create(canvas, function (p) {
                return bar(p, d.labels, [
                    { label: 'Reflect XR minutes', data: d.xr || [], backgroundColor: p.xrFill, stack: 's' },
                    { label: 'Reflect Mobile minutes', data: d.mobile || [], backgroundColor: p.mobileFill, stack: 's' }
                ], { scales: { x: { stacked: true }, y: { stacked: true, title: { display: true, text: 'Minutes', color: p.muted } } } });
            });
        },
        platformSplit: function (canvas, data) {
            var s = data.platform || {};
            ProviderCharts.create(canvas, function (p) {
                var o = ProviderCharts.baseOptions(p, { cutout: '62%', plugins: { legend: { position: 'bottom' } } });
                delete o.scales;
                return { type: 'doughnut', data: { labels: ['Reflect XR minutes', 'Reflect Mobile minutes'], datasets: [{ data: [s.xr || 0, s.mobile || 0], backgroundColor: [p.xrFill, p.mobileFill], borderWidth: 2 }] }, options: o };
            });
        },
        weeklyTrend: function (canvas, data) {
            var w = data.weekly || {};
            ProviderCharts.create(canvas, function (p) {
                return {
                    type: 'line',
                    data: {
                        labels: w.labels || [],
                        datasets: [
                            { label: 'Reflect XR', data: w.xr || [], borderColor: p.xr, backgroundColor: p.xr, pointStyle: 'circle', pointRadius: 3, tension: 0.3 },
                            { label: 'Reflect Mobile', data: w.mobile || [], borderColor: p.mobile, backgroundColor: p.mobile, borderDash: [6, 4], pointStyle: 'triangle', pointRadius: 4, tension: 0.3 }
                        ]
                    },
                    options: ProviderCharts.baseOptions(p, { scales: { y: { title: { display: true, text: 'Minutes per week', color: p.muted } } } })
                };
            });
        },
        modules: function (canvas, data) {
            var mods = data.modules || [];
            ProviderCharts.create(canvas, function (p) {
                return bar(p, mods.map(function (m) { return m.label; }), [{
                    label: 'Minutes (last 30 days)', data: mods.map(function (m) { return m.minutes; }),
                    backgroundColor: mods.map(function (m) { return m.xr ? p.xrFill : p.mobileFill; }), borderRadius: 4
                }], { indexAxis: 'y', plugins: { legend: { display: false }, tooltip: { callbacks: { afterLabel: function (c) { var m = mods[c.dataIndex]; return m.group + ', ' + m.sessions + (m.sessions === 1 ? ' session' : ' sessions'); } } } },
                    scales: { x: { grid: { display: true } }, y: { grid: { display: false } } } });
            });
        },
        groups: function (canvas, data) {
            var g = data.groups || [];
            ProviderCharts.create(canvas, function (p) {
                return bar(p, g.map(function (m) { return m.label; }), [{
                    label: 'Minutes (last 30 days)', data: g.map(function (m) { return m.minutes; }),
                    backgroundColor: g.map(function (m) { return m.xr ? p.xrFill : p.mobileFill; }), borderRadius: 4
                }], { indexAxis: 'y', plugins: { legend: { display: false } }, scales: { x: { grid: { display: true } }, y: { grid: { display: false } } } });
            });
        },
        timeOfDay: function (canvas, data) {
            var t = data.timeOfDay || {};
            ProviderCharts.create(canvas, function (p) {
                return bar(p, t.labels, [{ label: 'Sessions', data: t.counts || [], backgroundColor: (t.labels || []).map(function (l, i) { return i === 0 ? p.mobileFill : p.xrFill; }), borderRadius: 4 }], { plugins: { legend: { display: false } } });
            });
        },
        weekday: function (canvas, data) {
            var t = data.weekday || {};
            ProviderCharts.create(canvas, function (p) {
                return bar(p, t.labels, [{ label: 'Sessions', data: t.counts || [], backgroundColor: p.mobileFill, borderRadius: 4 }], { plugins: { legend: { display: false } } });
            });
        },
        mood: function (canvas, data) {
            var c = data.checkIns || {};
            ProviderCharts.create(canvas, function (p) {
                return prePost(p, c.labels, c.moodPre, c.moodPost, 'Mood before session', 'Mood after session', 'Mood (1-10, higher is better)', { min: 1, max: 10, beginAtZero: false });
            });
        },
        stress: function (canvas, data) {
            var c = data.checkIns || {};
            ProviderCharts.create(canvas, function (p) {
                return prePost(p, c.labels, c.stressPre, c.stressPost, 'Stress before session', 'Stress after session', 'Stress (1-10, lower is better)', { min: 1, max: 10, beginAtZero: false });
            });
        },
        assessments: function (canvas, data) {
            var a = data.assessments || {};
            ProviderCharts.create(canvas, function (p) {
                return {
                    type: 'line',
                    data: {
                        labels: a.labels || [],
                        datasets: [
                            { label: 'PHQ-9 (0-27)', data: a.phq || [], borderColor: p.success, backgroundColor: p.success, pointStyle: 'rectRot', pointRadius: 5, tension: 0.2 },
                            { label: 'GAD-7 (0-21)', data: a.gad || [], borderColor: p.warning, backgroundColor: p.warning, borderDash: [6, 4], pointStyle: 'triangle', pointRadius: 5, tension: 0.2 }
                        ]
                    },
                    options: ProviderCharts.baseOptions(p, { scales: { y: { max: 27, title: { display: true, text: 'Score (lower is better)', color: p.muted } } } })
                };
            });
        },
        heartRate: function (canvas, data) {
            var h = data.heartRate || {};
            ProviderCharts.create(canvas, function (p) {
                return prePost(p, h.labels, h.pre, h.post, 'Heart rate before session', 'Heart rate after session', 'beats/min', { beginAtZero: false });
            });
        },
        cumulativeMinutes: function (canvas, data) {
            var m = data.minutes || {};
            var labels = m.labels || [];
            var t = m.thresholds || [10, 20, 40];
            var flat = function (v) { return labels.map(function () { return v; }); };
            ProviderCharts.create(canvas, function (p) {
                return {
                    type: 'line',
                    data: {
                        labels: labels,
                        datasets: [
                            { label: 'Attested minutes (cumulative)', data: m.cumulative || [], borderColor: p.success, backgroundColor: p.success, stepped: true, pointRadius: 0, borderWidth: 3 },
                            { label: t[0] + ' min: 98979', data: flat(t[0]), borderColor: p.info, borderDash: [2, 4], pointRadius: 0, borderWidth: 1.5 },
                            { label: t[1] + ' min: 98980', data: flat(t[1]), borderColor: p.primary, borderDash: [6, 4], pointRadius: 0, borderWidth: 1.5 },
                            { label: t[2] + ' min: +98981', data: flat(t[2]), borderColor: p.warning, borderDash: [10, 4], pointRadius: 0, borderWidth: 1.5 }
                        ]
                    },
                    options: ProviderCharts.baseOptions(p, { scales: { y: { suggestedMax: t[2] + 5, title: { display: true, text: 'Minutes this month', color: p.muted } } } })
                };
            });
        }
    };

    function init(options) {
        var tabs = ProviderTabs.init({
            tabsId: options.tabsId,
            load: ClientDetailsService.loadTab,
            builders: builders,
            loadFailed: options.labels.loadFailed
        });

        // Live data for THIS client: refresh the status strip and the open tab.
        $(document).on('rpd:livedata', function (evt, events) {
            if (!events.some(function (e) { return e.clientId === options.clientId; })) return;
            var $strip = $('#clientStatusStrip');
            ClientDetailsService.loadTab($strip.attr('data-refresh-url')).then(function (html) { $strip.html(html); }, function () { });
            tabs.reloadActive();
        });
        ProviderLiveFeed.start({ feedUrl: options.feedUrl });

        // Links that jump to a tab (for example "Open RTM & billing").
        $(document).on('click', '.js-show-tab', function (e) {
            e.preventDefault();
            var el = document.querySelector('#' + options.tabsId + ' [data-bs-target="' + $(this).data('tab') + '"]');
            if (el) bootstrap.Tab.getOrCreateInstance(el).show();
        });

        ProviderReviewTimer.init(options);
    }

    return { init: init };
})(jQuery);
