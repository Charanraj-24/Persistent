/* Provider dashboard chart helper (Chart.js 4). Colors come from the theme's CSS tokens, so light/dark
   mode and the real theme work after merge. Charts redraw when data-theme-mode changes on <html>.
   Series are also told apart by line dash and point shape, never by color alone. */
window.ProviderCharts = (function () {
    'use strict';
    var registry = [];

    function token(name) {
        return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    }
    function rgb(name, alpha) {
        var v = token(name);
        return alpha === undefined ? 'rgb(' + v + ')' : 'rgba(' + v + ', ' + alpha + ')';
    }

    function palette() {
        return {
            text: token('--default-text-color') || '#1F305E',
            muted: token('--text-muted') || '#586F8A',
            grid: token('--default-border') || '#D8DCE1',
            xr: rgb('--success-rgb'), xrFill: rgb('--success-rgb', 0.85),
            mobile: rgb('--primary-rgb'), mobileFill: rgb('--primary-rgb', 0.85),
            info: rgb('--info-rgb'), warning: rgb('--warning-rgb'), primary: rgb('--primary-rgb'), success: rgb('--success-rgb')
        };
    }

    function baseOptions(p, extra) {
        var font = getComputedStyle(document.body).fontFamily;
        var o = {
            responsive: true, maintainAspectRatio: false, animation: false,
            interaction: { mode: 'index', intersect: false },
            plugins: {
                legend: { labels: { color: p.text, font: { family: font }, usePointStyle: true, boxWidth: 10 } },
                tooltip: { titleFont: { family: font }, bodyFont: { family: font } }
            },
            scales: {
                x: { ticks: { color: p.muted, font: { family: font }, maxRotation: 0, autoSkip: true, maxTicksLimit: 10 }, grid: { color: p.grid, display: false }, border: { color: p.grid } },
                y: { ticks: { color: p.muted, font: { family: font }, precision: 0 }, grid: { color: p.grid }, border: { color: p.grid }, beginAtZero: true }
            }
        };
        return deepMerge(o, extra || {});
    }

    function deepMerge(target, src) {
        Object.keys(src).forEach(function (k) {
            if (src[k] && typeof src[k] === 'object' && !Array.isArray(src[k])) {
                target[k] = deepMerge(target[k] || {}, src[k]);
            } else { target[k] = src[k]; }
        });
        return target;
    }

    /** Registers a chart factory: build(palette) returns a Chart.js config. */
    function create(canvas, build) {
        if (!canvas || typeof Chart === 'undefined') return null;
        var entry = { canvas: canvas, build: build, chart: null };
        entry.chart = new Chart(canvas, build(palette()));
        registry.push(entry);
        return entry.chart;
    }

    function redrawAll() {
        registry.forEach(function (e) {
            if (e.chart) e.chart.destroy();
            e.chart = new Chart(e.canvas, e.build(palette()));
        });
    }

    new MutationObserver(function (mutations) {
        if (mutations.some(function (m) { return m.attributeName === 'data-theme-mode'; })) redrawAll();
    }).observe(document.documentElement, { attributes: true });

    function readJson(id) {
        var el = document.getElementById(id);
        if (!el) return {};
        try { return JSON.parse(el.textContent); } catch (e) { return {}; }
    }

    return { create: create, baseOptions: baseOptions, readJson: readJson, palette: palette };
})();
