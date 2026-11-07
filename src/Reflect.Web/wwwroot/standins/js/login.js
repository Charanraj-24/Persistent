/* STAND-IN: sign-in page behavior (no jQuery needed). Delete on merge. */
(function () {
    'use strict';
    var email = document.getElementById('Email');
    var password = document.getElementById('Password');

    document.querySelectorAll('.js-fill-account').forEach(function (btn) {
        btn.addEventListener('click', function () {
            email.value = btn.getAttribute('data-email');
            document.querySelectorAll('.js-fill-account').forEach(function (b) { b.classList.toggle('is-selected', b === btn); });
            password.focus();
        });
    });

    var reveal = document.querySelector('.js-toggle-password');
    if (reveal) {
        reveal.addEventListener('click', function () {
            var show = password.type === 'password';
            password.type = show ? 'text' : 'password';
            reveal.setAttribute('aria-pressed', show ? 'true' : 'false');
            reveal.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
            reveal.querySelector('i').className = show ? 'ri-eye-off-line' : 'ri-eye-line';
        });
    }
})();
