/* STAND-IN: behavior for the lookalike layout shell (theme toggle, accessibility panel, sidebar collapse,
   TempData toasts). Delete on merge. */
(function (window, $) {
    'use strict';
    var root = document.documentElement;

    function save(key, value) { try { window.localStorage.setItem(key, value); } catch (e) { /* storage unavailable */ } }

    function setTheme(mode) {
        root.setAttribute('data-theme-mode', mode);
        save('reflect-theme', mode);
        $('.js-theme-icon').attr('class', 'js-theme-icon ' + (mode === 'dark' ? 'ri-sun-line' : 'ri-moon-line'));
        $('input[name="a11yTheme"][value="' + mode + '"]').prop('checked', true);
    }

    function setTextSize(pct) {
        root.style.fontSize = pct + '%';
        save('reflect-text-size', String(pct));
        $('#a11yTextSize').val(pct);
        $('#a11yTextSizeValue').text(pct + '%');
    }

    function setColorblind(on) {
        if (on) root.setAttribute('data-a11y-colorblind', 'true'); else root.removeAttribute('data-a11y-colorblind');
        save('reflect-colorblind', on ? 'true' : 'false');
        $('#a11yColorblind').prop('checked', on);
    }

    $(function () {
        setTheme(root.getAttribute('data-theme-mode') || 'light');
        setTextSize(parseInt(root.style.fontSize, 10) || 100);
        setColorblind(root.getAttribute('data-a11y-colorblind') === 'true');

        $('.js-theme-toggle').on('click', function () { setTheme(root.getAttribute('data-theme-mode') === 'dark' ? 'light' : 'dark'); });
        $('input[name="a11yTheme"]').on('change', function () { setTheme(this.value); });
        $('#a11yColorblind').on('change', function () { setColorblind(this.checked); });
        $('#a11yTextSize').on('input change', function () { setTextSize(parseInt(this.value, 10)); });
        $('.js-a11y-reset').on('click', function () { setTheme('light'); setTextSize(100); setColorblind(false); });

        $('.js-sidebar-collapse').on('click', function () {
            var collapsed = root.getAttribute('data-sidebar') === 'collapsed';
            if (collapsed) root.removeAttribute('data-sidebar'); else root.setAttribute('data-sidebar', 'collapsed');
            save('reflect-sidebar', collapsed ? 'open' : 'collapsed');
        });

        var $toast = $('#standinTempDataToast');
        if ($toast.length && $toast.data('message')) MessageService.showToastrMessage($toast.data('message'), $toast.data('type'));
    });
})(window, jQuery);
