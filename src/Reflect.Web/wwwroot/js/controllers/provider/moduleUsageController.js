/*
 * Provider > Dashboard > Module usage.
 * Two ways in: pick a module to see which clients use it most, or pick a client to see which modules they use most.
 * Filters (period, app, group, measure, search) and both sort orders apply instantly in the browser.
 * Live: when new Reflect data arrives the numbers are refreshed in place (filters, selection and typing are kept)
 * and anything that changed is highlighted. All text is inserted as text, never HTML.
 */
window.ModuleUsageController = (function ($) {
    'use strict';

    var STORE_KEY = 'rpd-usage-state';
    var DEFAULTS = { view: 'module', window: 1, platform: '', group: '', measure: 'min', q: '', leftSort: 'value', rightSort: 'value', sel: null, mapCol: 'total', mapDir: -1 };
    var state = load();
    var data = null, $root = null, prevValues = {}, changed = {};

    function load() {
        try { return $.extend({}, DEFAULTS, JSON.parse(window.sessionStorage.getItem(STORE_KEY) || '{}')); } catch (e) { return $.extend({}, DEFAULTS); }
    }
    function save() { try { window.sessionStorage.setItem(STORE_KEY, JSON.stringify(state)); } catch (e) { } }

    function readJson($pane) {
        var el = $pane.find('script.js-tab-data')[0];
        try { return el ? JSON.parse(el.textContent) : null; } catch (e) { return null; }
    }

    // ---------- formatting ----------
    function fmtValue(v) { return state.measure === 'min' ? v + ' min' : v + (v === 1 ? ' session' : ' sessions'); }
    function fmtAgo(ms) {
        if (!ms) return 'Not used';
        // Calendar days in US Eastern (not 24-hour blocks), so 7 PM yesterday reads "Yesterday".
        var dayOf = function (t) { var p = new Date(t).toLocaleDateString('en-CA', { timeZone: 'America/New_York' }).split('-'); return Date.UTC(+p[0], +p[1] - 1, +p[2]) / 86400000; };
        var days = dayOf(data.now) - dayOf(ms);
        var d = new Date(ms);
        var time = d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', timeZone: 'America/New_York' });
        if (days <= 0) return 'Today ' + time;
        if (days === 1) return 'Yesterday ' + time;
        return days + ' days ago';
    }
    function platformTag(p) { return p === 'xr' ? 'XR' : 'Mobile'; }

    // ---------- data shaping ----------
    function moduleOk(m) {
        if (state.platform && m.platform !== state.platform) return false;
        if (state.group && m.groupLabel !== state.group) return false;
        return true;
    }
    function textMatch(s) { return !state.q || String(s).toLowerCase().indexOf(state.q.toLowerCase()) >= 0; }
    function val(u) { return u[state.measure][state.window]; }

    /** Usage rows passing the filters (with a value > 0 in the chosen period). */
    function rows() {
        return data.usage.filter(function (u) {
            var m = data.modules[u.m], c = data.clients[u.c];
            if (!moduleOk(m) || val(u) <= 0) return false;
            return textMatch(c.name) || textMatch(m.name) || textMatch(m.groupLabel);
        });
    }

    /** Each client's total in the period across every module (for "share of their time"). */
    function clientTotals() {
        var t = {};
        data.usage.forEach(function (u) { t[u.c] = (t[u.c] || 0) + val(u); });
        return t;
    }

    function aggregate(rs, byModule) {
        var map = {};
        rs.forEach(function (u) {
            var k = byModule ? u.m : u.c;
            var a = map[k] || (map[k] = { idx: k, value: 0, reach: 0, last: 0 });
            a.value += val(u); a.reach += 1; a.last = Math.max(a.last, u.last);
        });
        return map;
    }

    function sorter(kind, labelOf) {
        return function (a, b) {
            if (kind === 'name') return labelOf(a).localeCompare(labelOf(b));
            if (kind === 'reach' && b.reach !== a.reach) return b.reach - a.reach;
            if (kind === 'recent' && b.last !== a.last) return b.last - a.last;
            if (kind === 'share' && b.share !== a.share) return b.share - a.share;
            return b.value - a.value || labelOf(a).localeCompare(labelOf(b));
        };
    }

    // ---------- rendering ----------
    function renderGroupOptions() {
        var $g = $root.find('#usageGroup').empty().append($('<option value=""/>').text('All groups'));
        var seen = {};
        data.modules.forEach(function (m) {
            if (state.platform && m.platform !== state.platform) return;
            if (seen[m.groupLabel]) return;
            seen[m.groupLabel] = true;
            $g.append($('<option/>').val(m.groupLabel).text(m.groupLabel));
        });
        if (state.group && !seen[state.group]) state.group = '';
        $g.val(state.group);
    }

    function syncControls() {
        $root.find('.js-usage-view').each(function () {
            var on = $(this).data('view') === state.view;
            $(this).toggleClass('active', on).attr('aria-checked', on ? 'true' : 'false');
        });
        $root.find('#usagePeriod').val(String(state.window));
        $root.find('#usagePlatform').val(state.platform);
        $root.find('#usageMeasure').val(state.measure);
        if ($root.find('#usageSearch').val() !== state.q) $root.find('#usageSearch').val(state.q);
        $root.find('[data-key="leftSort"]').val(state.leftSort);
        $root.find('[data-key="rightSort"]').val(state.rightSort);
        renderGroupOptions();
    }

    function bar(fraction) {
        return $('<span class="rpd-usage-bar" aria-hidden="true"/>').append($('<span/>').css('width', Math.max(2, Math.round(fraction * 100)) + '%'));
    }

    function clientLink(c) {
        return $('<a class="rpd-name"/>').attr('href', $root.attr('data-client-url').replace('__id__', c.id)).text(c.name);
    }

    function render() {
        if (!data) return;
        var byModule = state.view === 'module';
        var rs = rows();
        var totals = clientTotals();
        var agg = aggregate(rs, byModule);
        var periodWord = ['7', '30', '90'][state.window];

        // Left list: modules (or clients), including filtered modules nobody used, so gaps are visible.
        var items = [];
        if (byModule) {
            data.modules.forEach(function (m, i) {
                if (!moduleOk(m) || !(textMatch(m.name) || textMatch(m.groupLabel) || agg[i])) return;
                items.push(agg[i] || { idx: i, value: 0, reach: 0, last: 0 });
            });
        } else {
            data.clients.forEach(function (c, i) { if (agg[i] || textMatch(c.name)) items.push(agg[i] || { idx: i, value: 0, reach: 0, last: 0 }); });
        }
        var labelOf = byModule ? function (a) { return data.modules[a.idx].name; } : function (a) { return data.clients[a.idx].name; };
        items.sort(sorter(state.leftSort, labelOf));
        var max = items.reduce(function (m, a) { return Math.max(m, a.value); }, 0) || 1;

        var selKey = byModule ? 'm' : 'c';
        var sel = state.sel && state.sel[selKey] != null && items.some(function (a) { return a.idx === state.sel[selKey]; }) ? state.sel[selKey] : (items.length && items[0].value > 0 ? items[0].idx : null);
        state.sel = state.sel || {}; state.sel[selKey] = sel;

        $root.find('.js-usage-left-title').text(byModule ? 'Most used modules, last ' + periodWord + ' days' : 'Most active clients, last ' + periodWord + ' days');
        var $left = $root.find('.js-usage-left').empty();
        if (!items.length) $left.append($('<li class="rpd-usage-empty"/>').text('Nothing matches these filters.'));
        items.forEach(function (a) {
            var isSel = a.idx === sel;
            var $li = $('<li role="option" tabindex="0" class="rpd-usage-item"/>').attr('aria-selected', isSel ? 'true' : 'false').attr('data-idx', a.idx)
                .toggleClass('is-selected', isSel).toggleClass('is-zero', a.value === 0)
                .toggleClass('is-changed', byModule ? !!changed['m' + a.idx] : !!changed['c' + a.idx]);
            var $top = $('<div class="rpd-usage-item-top"/>');
            if (byModule) {
                var m = data.modules[a.idx];
                $top.append($('<span class="rpd-usage-item-name"/>').text(m.name))
                    .append($('<span class="rpd-tag"/>').toggleClass('is-xr', m.platform === 'xr').text(m.group + ' · ' + platformTag(m.platform)));
            } else {
                var c = data.clients[a.idx];
                $top.append($('<span class="avatar avatar-sm avatar-rounded"/>').text(c.initials)).append($('<span class="rpd-usage-item-name"/>').text(c.name));
            }
            $top.append($('<span class="rpd-usage-item-value"/>').text(a.value ? fmtValue(a.value) : 'Not used'));
            var reach = byModule ? a.reach + (a.reach === 1 ? ' client' : ' clients') : a.reach + (a.reach === 1 ? ' module' : ' modules');
            $li.append($top).append(bar(a.value / max))
                .append($('<div class="rpd-usage-item-meta"/>').text(a.value ? reach + ' · last used ' + fmtAgo(a.last).toLowerCase() : 'No use in this period'));
            $left.append($li);
        });

        renderRight(byModule, sel, rs, totals, periodWord);
        renderMap(totals);
    }

    function renderRight(byModule, sel, rs, totals, periodWord) {
        var $right = $root.find('.js-usage-right').empty();
        var $title = $root.find('.js-usage-right-title'), $sub = $root.find('.js-usage-right-sub');
        if (sel == null) { $title.text('Pick an item on the left'); $sub.text(''); $right.append($('<div class="rpd-usage-empty"/>').text('No usage matches these filters.')); return; }

        var list = rs.filter(function (u) { return byModule ? u.m === sel : u.c === sel; }).map(function (u) {
            var total = totals[u.c] || 0;
            return { u: u, idx: byModule ? u.c : u.m, value: val(u), ses: u.ses[state.window], min: u.min[state.window], last: u.last, share: total ? val(u) / total : 0 };
        });
        var labelOf = byModule ? function (a) { return data.clients[a.idx].name; } : function (a) { return data.modules[a.idx].name; };
        list.sort(sorter(state.rightSort, labelOf));
        var max = list.reduce(function (m, a) { return Math.max(m, a.value); }, 0) || 1;

        if (byModule) {
            var m = data.modules[sel];
            $title.text('Who uses ' + m.name + ' most');
            var nonUsers = data.clients.length - list.length;
            $sub.text(m.groupLabel + (m.ai ? ', uses AI image generation' : '') + '. ' + list.length + ' of ' + data.clients.length + ' clients in the last ' + periodWord + ' days' + (nonUsers ? ', ' + nonUsers + ' have not used it.' : '.'));
        } else {
            var c = data.clients[sel];
            $title.empty().append(document.createTextNode('What ')).append(clientLink(c)).append(document.createTextNode(' uses most'));
            $sub.text(list.length + (list.length === 1 ? ' module' : ' modules') + ' in the last ' + periodWord + ' days, ' + fmtValue(totals[sel] || 0) + ' in total across Reflect.');
        }

        var $t = $('<table class="table table-sm align-middle mb-0 rpd-usage-detail"/>');
        $t.append($('<thead/>').append($('<tr/>')
            .append($('<th/>').text(byModule ? 'Client' : 'Module'))
            .append($('<th class="w-100"/>').text(state.measure === 'min' ? 'Minutes' : 'Sessions'))
            .append($('<th class="text-end"/>').text('Sessions'))
            .append($('<th class="text-end" title="Share of their total time in Reflect in this period"/>').text('Share of time'))
            .append($('<th/>').text('Last used'))));
        var $b = $('<tbody/>');
        list.forEach(function (a) {
            var $name = byModule
                ? $('<td class="text-nowrap"/>').append($('<span class="avatar avatar-sm avatar-rounded me-2"/>').text(data.clients[a.idx].initials)).append(clientLink(data.clients[a.idx]))
                : $('<td class="text-nowrap"/>').append($('<span class="fw-semibold"/>').text(data.modules[a.idx].name)).append($('<span class="text-muted fs-12 d-block"/>').text(data.modules[a.idx].groupLabel));
            var key = a.u.c + '-' + a.u.m;
            $b.append($('<tr/>').toggleClass('is-changed', !!changed[key])
                .append($name)
                .append($('<td/>').append($('<div class="d-flex align-items-center gap-2"/>').append(bar(a.value / max)).append($('<span class="text-nowrap fw-semibold"/>').text(fmtValue(a.value)))))
                .append($('<td class="text-end"/>').text(a.ses))
                .append($('<td class="text-end"/>').text(Math.round(a.share * 100) + '%'))
                .append($('<td class="text-nowrap text-muted fs-12"/>').text(fmtAgo(a.last))));
        });
        $t.append($b);
        $right.append($('<div class="table-responsive"/>').append($t));
    }

    function renderMap(totals) {
        // Columns: groups (or the modules of the chosen group).
        var cols = [], seen = {};
        data.modules.forEach(function (m, i) {
            if (!moduleOk(m)) return;
            if (state.group) { cols.push({ key: 'm' + i, label: m.name, test: function (u) { return u.m === i; } }); return; }
            if (seen[m.groupLabel]) return;
            seen[m.groupLabel] = true;
            cols.push({ key: 'g' + m.groupLabel, label: m.groupLabel, test: function (u) { return data.modules[u.m].groupLabel === m.groupLabel; } });
        });
        $root.find('.js-usage-map-axis').text(state.group ? 'module in ' + state.group : 'module group');

        var rowsOut = data.clients.map(function (c, ci) {
            var cells = cols.map(function (col) {
                var v = 0, chg = false;
                data.usage.forEach(function (u) { if (u.c === ci && moduleOk(data.modules[u.m]) && col.test(u)) { v += val(u); chg = chg || !!changed[u.c + '-' + u.m]; } });
                return { v: v, changed: chg };
            });
            return { c: c, ci: ci, cells: cells, total: cells.reduce(function (s, x) { return s + x.v; }, 0) };
        }).filter(function (r) { return textMatch(r.c.name) || !state.q || r.total > 0; });

        var colIdx = state.mapCol === 'total' || state.mapCol === 'name' ? -1 : cols.findIndex(function (c) { return c.key === state.mapCol; });
        if (state.mapCol !== 'total' && state.mapCol !== 'name' && colIdx < 0) state.mapCol = 'total';
        rowsOut.sort(function (a, b) {
            var d;
            if (state.mapCol === 'name') d = a.c.name.localeCompare(b.c.name);
            else if (state.mapCol === 'total') d = a.total - b.total;
            else d = a.cells[colIdx].v - b.cells[colIdx].v;
            return (state.mapCol === 'name' ? 1 : state.mapDir) * d || a.c.name.localeCompare(b.c.name);
        });
        var max = 1;
        rowsOut.forEach(function (r) { r.cells.forEach(function (x) { max = Math.max(max, x.v); }); });

        var $t = $root.find('.js-usage-map').empty();
        var $hr = $('<tr/>');
        function head(key, label, cls) {
            var active = state.mapCol === key;
            var arrow = active ? (key === 'name' ? ' ▲' : state.mapDir < 0 ? ' ▼' : ' ▲') : '';
            return $('<th scope="col"/>').addClass(cls || '').attr('aria-sort', active ? (key === 'name' || state.mapDir > 0 ? 'ascending' : 'descending') : 'none')
                .append($('<button type="button" class="rpd-usage-sort js-usage-sort"/>').attr('data-col', key).text(label + arrow));
        }
        $hr.append(head('name', 'Client', 'rpd-usage-map-name'));
        cols.forEach(function (col) { $hr.append(head(col.key, col.label)); });
        $hr.append(head('total', 'Total', 'text-end'));
        $t.append($('<thead/>').append($hr));
        var $b = $('<tbody/>');
        rowsOut.forEach(function (r) {
            var $tr = $('<tr/>');
            $tr.append($('<th scope="row" class="rpd-usage-map-name"/>').append(clientLink(r.c)));
            r.cells.forEach(function (x) {
                var level = x.v ? Math.min(5, Math.ceil((x.v / max) * 5)) : 0;
                $tr.append($('<td class="rpd-heat"/>').addClass('rpd-heat-' + level).toggleClass('is-changed', x.changed).text(x.v || ''));
            });
            $tr.append($('<td class="text-end fw-semibold"/>').text(r.total || ''));
            $b.append($tr);
        });
        $t.append($b);
    }

    // ---------- live refresh ----------
    function snapshotValues() {
        var v = {};
        data.usage.forEach(function (u) { v[u.c + '-' + u.m] = u.ses[2] + ':' + u.min[2]; });
        return v;
    }

    function applyData(next, isRefresh) {
        var before = data ? snapshotValues() : null;
        data = next;
        changed = {};
        if (isRefresh && before) {
            var after = snapshotValues(), n = 0;
            Object.keys(after).forEach(function (k) {
                if (before[k] !== after[k]) {
                    changed[k] = true; n++;
                    var parts = k.split('-'); changed['c' + parts[0]] = true; changed['m' + parts[1]] = true;
                }
            });
            var t = new Date().toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', second: '2-digit' });
            $root.find('.js-usage-updated').text(n ? 'Updated ' + t + ': ' + n + (n === 1 ? ' change' : ' changes') + ' highlighted' : 'Updated ' + t);
            window.setTimeout(function () { changed = {}; $root.find('.is-changed').removeClass('is-changed'); }, 6000);
        }
    }

    function refresh(url) {
        return $.ajax({ url: url, type: 'GET', cache: false }).then(function (html) {
            var next = readJson($('<div/>').append($.parseHTML(html, document, false)));
            if (!next || !$root) return;
            applyData(next, true);
            render();
        });
    }

    // ---------- wiring ----------
    function mount($pane) {
        $root = $pane.find('#usageExplorer');
        if (!$root.length) return;
        applyData(readJson($pane), false);
        syncControls();
        render();

        $root.on('click', '.js-usage-view', function () { state.view = $(this).data('view'); save(); syncControls(); render(); });
        $root.on('change input', '.js-usage-filter', function (e) {
            var key = $(this).data('key'), v = $(this).val();
            if (e.type === 'input' && key !== 'q') return;
            state[key] = key === 'window' ? parseInt(v, 10) : v;
            if (key === 'platform') renderGroupOptions();
            save(); render();
        });
        $root.on('click', '.js-usage-reset', function () { state = $.extend({}, DEFAULTS); save(); syncControls(); render(); });
        function pick($li) {
            var idx = parseInt($li.attr('data-idx'), 10);
            state.sel = state.sel || {};
            state.sel[state.view === 'module' ? 'm' : 'c'] = idx;
            save(); render();
            $root.find('.js-usage-left [data-idx="' + idx + '"]').trigger('focus');
        }
        $root.on('click', '.rpd-usage-item', function () { pick($(this)); });
        $root.on('keydown', '.rpd-usage-item', function (e) {
            if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); pick($(this)); }
            else if (e.key === 'ArrowDown') { e.preventDefault(); $(this).next('.rpd-usage-item').trigger('focus'); }
            else if (e.key === 'ArrowUp') { e.preventDefault(); $(this).prev('.rpd-usage-item').trigger('focus'); }
        });
        $root.on('click', '.js-usage-sort', function () {
            var col = $(this).attr('data-col');
            if (state.mapCol === col && col !== 'name') state.mapDir = -state.mapDir; else { state.mapCol = col; state.mapDir = -1; }
            save(); render();
        });
    }

    return { mount: mount, refresh: refresh };
})(jQuery);
