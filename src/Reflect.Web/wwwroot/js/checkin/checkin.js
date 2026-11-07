/* Public phone check-in: live slider values and a 3-word limit. No dependencies. */
(function () {
    'use strict';
    document.querySelectorAll('.js-range').forEach(function (r) {
        var out = document.querySelector('.js-out[for="' + r.id + '"]');
        var sync = function () { if (out) out.textContent = r.value; r.setAttribute('aria-valuetext', r.value + ' of 10'); };
        r.addEventListener('input', sync); sync();
    });
    var boxes = Array.prototype.slice.call(document.querySelectorAll('.js-emotion'));
    function limit() {
        var n = boxes.filter(function (b) { return b.checked; }).length;
        boxes.forEach(function (b) { b.disabled = !b.checked && n >= 3; b.parentElement.classList.toggle('is-disabled', b.disabled); });
    }
    boxes.forEach(function (b) { b.addEventListener('change', limit); }); limit();
    var code = document.getElementById('code');
    if (code) code.addEventListener('input', function () { code.value = code.value.toUpperCase().replace(/[^A-Z0-9]/g, ''); });
    var form = document.querySelector('form[method="post"]');
    if (form) form.addEventListener('submit', function (e) {
        var sel = form.querySelector('select');
        if (sel && !sel.value) { e.preventDefault(); sel.focus(); sel.classList.add('is-invalid'); return; }
        var btn = form.querySelector('button[type="submit"]'); if (btn) { btn.disabled = true; btn.textContent = 'Sending...'; }
    });
})();
