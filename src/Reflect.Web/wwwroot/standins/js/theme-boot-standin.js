/* STAND-IN (delete on merge): restores theme and sidebar state before first paint to avoid a flash. */
(function () {
    var root = document.documentElement;
    try {
        var theme = window.localStorage.getItem('rpd-theme');
        if (!theme && window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) theme = 'dark';
        if (theme) { root.setAttribute('data-theme-mode', theme); root.setAttribute('data-bs-theme', theme); }
        if (window.localStorage.getItem('rpd-sidebar') === 'collapsed') root.setAttribute('data-sidebar', 'collapsed');
        var size = parseInt(window.localStorage.getItem('rpd-text-size'), 10);
        if (size) root.style.fontSize = size + '%';
        if (window.localStorage.getItem('rpd-colorblind') === 'true') root.setAttribute('data-a11y-colorblind', 'true');
    } catch (e) { /* storage unavailable */ }
})();
