/* Provider dashboard: lazy-loaded tabs and in-card view toggles.
   - A .tab-pane with data-tab-url is fetched the first time its tab is shown (one request per tab).
   - Charts are <canvas data-chart="name">, built from the pane's <script type="application/json" class="js-tab-data">
     only when the canvas is visible (so hidden views never render at zero size).
   - Tables with class rpd-dt get the same DataTables options as the shared dt-client helper.
   - Buttons .js-view-toggle switch panels marked data-view-panel="group:key". */
window.ProviderTabs = (function ($) {
    'use strict';

    var toggleWired = false;

    function initTables($root) {
        $root.find('table.rpd-dt').each(function () {
            if ($.fn.dataTable.isDataTable(this)) return;
            var $t = $(this);
            var lastHeader = $.trim($t.find('thead th').last().text());
            var opts = { responsive: true, autoWidth: false, language: { search: '', searchPlaceholder: 'Search...' } };
            if (lastHeader === 'Actions') opts.columnDefs = [{ targets: -1, orderable: false }];
            $t.DataTable(opts);
        });
    }

    function adjustTables($root) {
        $root.find('table.rpd-dt').each(function () {
            if ($.fn.dataTable.isDataTable(this)) $(this).DataTable().columns.adjust().responsive.recalc();
        });
    }

    function readData($pane) {
        var el = $pane.find('script.js-tab-data')[0];
        if (!el) return {};
        try { return JSON.parse(el.textContent); } catch (e) { return {}; }
    }

    /** Builds every not-yet-built, visible chart canvas inside $scope. */
    function buildCharts($scope, builders, data) {
        $scope.find('canvas[data-chart]').each(function () {
            var $c = $(this);
            if ($c.data('built') || $c.closest('.d-none').length) return;
            var build = builders[$c.attr('data-chart')];
            if (!build) return;
            $c.data('built', true);
            build(this, data);
        });
    }

    function wireViewToggles() {
        if (toggleWired) return;
        toggleWired = true;
        $(document).on('click', '.js-view-toggle', function () {
            var $btn = $(this);
            var group = $btn.attr('data-view-group');
            var key = $btn.attr('data-view');
            var $scope = $btn.closest('.tab-pane, .rpd-view-scope');
            if (!$scope.length) $scope = $(document.body);

            $scope.find('.js-view-toggle[data-view-group="' + group + '"]').each(function () {
                var on = $(this).attr('data-view') === key;
                $(this).toggleClass('active', on).attr('aria-pressed', on ? 'true' : 'false');
            });
            $scope.find('[data-view-panel^="' + group + ':"]').each(function () {
                $(this).toggleClass('d-none', $(this).attr('data-view-panel') !== group + ':' + key);
            });
            var $shown = $scope.find('[data-view-panel="' + group + ':' + key + '"]');
            $scope.trigger('rpd:viewshown', [$shown]);
            adjustTables($shown);
        });
    }

    /**
     * options: tabsId, load(url) -> Promise<html>, builders { chartName: fn(canvas, data) }, loadFailed (text),
     *          onLoaded(pane) optional.
     */
    function init(options) {
        wireViewToggles();
        var $tabs = $('#' + options.tabsId);

        function load(pane) {
            var $pane = $(pane);
            var url = $pane.attr('data-tab-url');
            if (!url || $pane.data('state') === 'loading' || $pane.data('state') === 'loaded') return;
            $pane.data('state', 'loading').attr('aria-busy', 'true');

            options.load(url).then(function (html) {
                $pane.html(html).data('state', 'loaded').attr('aria-busy', 'false');
                var data = readData($pane);
                initTables($pane);
                buildCharts($pane, options.builders || {}, data);
                $pane.off('rpd:viewshown').on('rpd:viewshown', function (e, $shown) { buildCharts($shown, options.builders || {}, data); });
                if (options.onLoaded) options.onLoaded(pane);
            }, function () {
                $pane.data('state', 'error').attr('aria-busy', 'false').empty().append(
                    $('<div class="rpd-tab-loading" role="alert"/>')
                        .append($('<i class="ri-error-warning-line text-danger" aria-hidden="true"/>'))
                        .append($('<span/>').text(options.loadFailed || 'This section could not be loaded.'))
                        .append($('<button type="button" class="btn btn-sm btn-outline-secondary js-tab-retry"/>').text('Try again'))
                );
            });
        }

        $tabs.on('shown.bs.tab', '[data-bs-toggle="tab"]', function (e) {
            var pane = document.querySelector($(e.target).attr('data-bs-target'));
            if (pane) { load(pane); adjustTables($(pane)); }
        });
        $(document).on('click', '.js-tab-retry', function () {
            var pane = $(this).closest('.tab-pane')[0];
            $(pane).data('state', '');
            load(pane);
        });

        // Restore the remembered (or #hash) tab, then load whichever tab is active.
        initTabPersistence(options.tabsId);
        var active = $tabs.find('.nav-link.active').attr('data-bs-target');
        if (active) load(document.querySelector(active));

        function reload(target) { var p = document.querySelector(target); if (p) { $(p).data('state', ''); load(p); } }
        return {
            reload: reload,
            /** Re-fetch the visible tab and mark the others stale so they reload next time they open. */
            reloadActive: function () {
                $('#' + options.tabsId + 'Content > .tab-pane').each(function () { if (!$(this).hasClass('active')) $(this).data('state', ''); });
                var active = $tabs.find('.nav-link.active').attr('data-bs-target');
                if (active) reload(active);
            }
        };
    }

    /**
     * For server-rendered tabs (no lazy loading): charts in every pane under #tabsId are built when visible,
     * from one JSON blob, and toggles work the same way.
     */
    function bindStatic(tabsId, builders, data) {
        wireViewToggles();
        var $content = $('#' + tabsId + 'Content');
        $content.on('rpd:viewshown', function (e, $shown) { buildCharts($shown, builders, data); });
        $('#' + tabsId).on('shown.bs.tab', '[data-bs-toggle="tab"]', function (e) {
            buildCharts($($(e.target).attr('data-bs-target')), builders, data);
        });
        initTabPersistence(tabsId);
        initTabDataTableFix('#' + tabsId);
        buildCharts($content, builders, data);
    }

    return { init: init, bindStatic: bindStatic, initTables: initTables };
})(jQuery);
