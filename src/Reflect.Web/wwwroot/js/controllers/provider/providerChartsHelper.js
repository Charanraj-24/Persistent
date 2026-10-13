/*
 * ProviderChartsHelper: shared Chart.js setup for Provider Dashboard screens.
 * Reads every color from the theme tokens, rebuilds charts when light/dark mode changes,
 * and adds a "Table" view to each chart so no value depends on color alone.
 */
var ProviderChartsHelper = (function ($) {
    'use strict';

    var builders = [];
    var observerStarted = false;

    function css(name) {
        return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    }

    function rgbToken(name, alpha) {
        var channels = css(name);
        return alpha === undefined ? 'rgb(' + channels + ')' : 'rgba(' + channels + ', ' + alpha + ')';
    }

    function palette() {
        return {
            xr: css('--rpd-series-xr'),
            mobile: css('--rpd-series-mobile'),
            text: css('--text-muted'),
            strongText: css('--default-text-color'),
            grid: css('--default-border'),
            surface: css('--custom-white'),
            warning: rgbToken('--warning-rgb'),
            info: rgbToken('--info-rgb')
        };
    }

    function withAlpha(hex, alpha) {
        var h = hex.replace('#', '');
        if (h.length === 3) h = h.split('').map(function (c) { return c + c; }).join('');
        var n = parseInt(h, 16);
        return 'rgba(' + ((n >> 16) & 255) + ', ' + ((n >> 8) & 255) + ', ' + (n & 255) + ', ' + alpha + ')';
    }

    function baseOptions(p) {
        return {
            responsive: true,
            maintainAspectRatio: false,
            animation: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? false : { duration: 450 },
            interaction: { mode: 'index', intersect: false },
            plugins: {
                legend: { display: false },
                tooltip: {
                    backgroundColor: p.surface,
                    titleColor: p.strongText,
                    bodyColor: p.strongText,
                    borderColor: p.grid,
                    borderWidth: 1,
                    padding: 10,
                    cornerRadius: 8,
                    boxPadding: 4,
                    usePointStyle: true
                }
            },
            scales: {
                x: {
                    grid: { display: false },
                    border: { color: p.grid },
                    ticks: { color: p.text, maxRotation: 0, autoSkipPadding: 12 }
                },
                y: {
                    beginAtZero: true,
                    grid: { color: p.grid, lineWidth: 1 },
                    border: { display: false },
                    ticks: { color: p.text, precision: 0, maxTicksLimit: 6 }
                }
            }
        };
    }

    function applyDefaults() {
        if (!window.Chart) return;
        var font = css('--default-font-family') || 'sans-serif';
        Chart.defaults.font.family = font;
        Chart.defaults.font.size = parseFloat(getComputedStyle(document.documentElement).fontSize) * 0.75;
        Chart.defaults.color = css('--text-muted');
    }

    function render(entry) {
        if (entry.chart) entry.chart.destroy();
        var canvas = document.getElementById(entry.canvasId);
        if (!canvas) return;
        var p = palette();
        entry.chart = new Chart(canvas, entry.build(p, baseOptions(p)));
    }

    function startObserver() {
        if (observerStarted) return;
        observerStarted = true;
        var timer = null;
        new MutationObserver(function () {
            clearTimeout(timer);
            timer = setTimeout(function () {
                applyDefaults();
                builders.forEach(render);
            }, 60);
        }).observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme-mode', 'style'] });
    }

    /** Registers a chart. build(palette, baseOptions) returns a Chart.js config. */
    function register(canvasId, build) {
        applyDefaults();
        var entry = { canvasId: canvasId, build: build, chart: null };
        builders.push(entry);
        render(entry);
        startObserver();
        return entry;
    }

    /** Wires a "Table" toggle that swaps a chart for an accessible data table built from the same values. */
    function tableToggle(buttonId, chartContainerId, tableContainerId, headers, rows) {
        var $btn = $('#' + buttonId);
        var $chart = $('#' + chartContainerId);
        var $tableHost = $('#' + tableContainerId);
        var built = false;

        $btn.on('click', function () {
            if (!built) {
                var $table = $('<table class="table table-sm table-bordered align-middle mb-0"></table>');
                var $thead = $('<thead><tr></tr></thead>');
                headers.forEach(function (h) { $thead.find('tr').append($('<th scope="col"></th>').text(h)); });
                var $tbody = $('<tbody></tbody>');
                rows.forEach(function (r) {
                    var $tr = $('<tr></tr>');
                    r.forEach(function (cell, i) {
                        $tr.append($(i === 0 ? '<th scope="row"></th>' : '<td></td>').text(cell === null || cell === undefined ? '-' : cell));
                    });
                    $tbody.append($tr);
                });
                $table.append($thead).append($tbody);
                $tableHost.append($('<div class="table-responsive"></div>').append($table));
                built = true;
            }
            var showing = $btn.attr('aria-pressed') === 'true';
            $btn.attr('aria-pressed', showing ? 'false' : 'true');
            $btn.find('.js-toggle-label').text(showing ? 'Table' : 'Chart');
            $btn.find('i').attr('class', showing ? 'ri-table-line me-1' : 'ri-bar-chart-2-line me-1');
            $chart.toggleClass('d-none', !showing);
            $tableHost.toggleClass('show', !showing);
        });
    }

    /** Bar dataset with thin bars, rounded data-end, and a surface-colored gap between stacked segments. */
    function barDataset(label, data, color, p, stack) {
        return {
            label: label,
            data: data,
            backgroundColor: color,
            hoverBackgroundColor: color,
            borderColor: p.surface,
            borderWidth: stack ? { top: 2 } : 0,
            borderSkipped: 'start',
            borderRadius: 4,
            maxBarThickness: 24,
            stack: stack || undefined
        };
    }

    /** 2px line with ringed markers. */
    function lineDataset(label, data, color, p, fill) {
        return {
            label: label,
            data: data,
            borderColor: color,
            backgroundColor: fill ? withAlpha(color, 0.1) : color,
            fill: !!fill,
            borderWidth: 2,
            tension: 0.3,
            cubicInterpolationMode: 'monotone',
            pointRadius: 4,
            pointHoverRadius: 6,
            pointBackgroundColor: color,
            pointBorderColor: p.surface,
            pointBorderWidth: 2,
            spanGaps: true
        };
    }

    return {
        register: register,
        tableToggle: tableToggle,
        barDataset: barDataset,
        lineDataset: lineDataset,
        withAlpha: withAlpha,
        css: css,
        rgbToken: rgbToken
    };
})(jQuery);
