/* ==========================================================================
   STAND-IN LAYOUT BEHAVIOR (delete on merge)
   Theme toggle, accessibility panel, and sidebar collapse for the stand-in shell.
   ========================================================================== */
(function (window, $) {
    'use strict';

    var root = document.documentElement;
    var store = {
        get: function (key) { try { return window.localStorage.getItem(key); } catch (e) { return null; } },
        set: function (key, value) { try { window.localStorage.setItem(key, value); } catch (e) { /* ignore */ } }
    };

    function applyTheme(mode) {
        root.setAttribute('data-theme-mode', mode);
        root.setAttribute('data-bs-theme', mode);
        $('.js-theme-icon').attr('class', 'js-theme-icon ' + (mode === 'dark' ? 'ri-sun-line' : 'ri-moon-line'));
        $('input[name="a11yTheme"][value="' + mode + '"]').prop('checked', true);
    }

    function applyTextSize(percent) {
        root.style.fontSize = percent + '%';
        $('#a11yTextSize').val(percent);
        $('#a11yTextSizeValue').text(percent + '%');
    }

    function applyColorBlind(on) {
        if (on) root.setAttribute('data-a11y-colorblind', 'true'); else root.removeAttribute('data-a11y-colorblind');
        $('#a11yColorBlind').prop('checked', on);
    }

    $(function () {
        applyTheme(root.getAttribute('data-theme-mode') || 'light');
        applyTextSize(parseInt(store.get('rpd-text-size'), 10) || 100);
        applyColorBlind(store.get('rpd-colorblind') === 'true');

        $('#themeToggle').on('click', function () {
            var next = root.getAttribute('data-theme-mode') === 'dark' ? 'light' : 'dark';
            applyTheme(next);
            store.set('rpd-theme', next);
        });

        $('input[name="a11yTheme"]').on('change', function () {
            applyTheme(this.value);
            store.set('rpd-theme', this.value);
        });

        $('#a11yTextSize').on('input change', function () {
            applyTextSize(this.value);
            store.set('rpd-text-size', this.value);
        });

        $('#a11yColorBlind').on('change', function () {
            applyColorBlind(this.checked);
            store.set('rpd-colorblind', this.checked ? 'true' : 'false');
        });

        $('#a11yReset').on('click', function () {
            applyTextSize(100); store.set('rpd-text-size', '100');
            applyColorBlind(false); store.set('rpd-colorblind', 'false');
        });

        $('#sidebarToggle').on('click', function () {
            if (window.matchMedia('(max-width: 991.98px)').matches) {
                var open = root.getAttribute('data-sidebar-open') === 'true';
                root.setAttribute('data-sidebar-open', open ? 'false' : 'true');
            } else {
                var collapsed = root.getAttribute('data-sidebar') === 'collapsed';
                if (collapsed) root.removeAttribute('data-sidebar'); else root.setAttribute('data-sidebar', 'collapsed');
                store.set('rpd-sidebar', collapsed ? 'expanded' : 'collapsed');
            }
        });

        $('#sidebarBackdrop').on('click', function () {
            root.setAttribute('data-sidebar-open', 'false');
        });
    });
})(window, jQuery);
