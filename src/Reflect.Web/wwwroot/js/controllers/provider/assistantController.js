/*
 * Clinical assistant: a centered panel toggled with Ctrl + Shift (both pressed, then released, with no other key).
 * Opening sends a wave across the screen. Esc and the close button also close it. The conversation is loaded from
 * the server, so it is still there after closing, reloading or coming back to the page. Answers render as text only.
 */
window.AssistantController = (function ($) {
    'use strict';

    /** Plain text to safe DOM: paragraphs, and lines starting with "- " or "* " become a list. */
    function renderAnswer(text) {
        var $wrap = $('<div/>');
        var $list = null;
        String(text || '').split(/\r?\n/).forEach(function (raw) {
            var line = raw.trim();
            if (!line) { $list = null; return; }
            var bullet = /^([-*•]|\d+\.)\s+/.exec(line);
            var clean = line.replace(/^([-*•]|\d+\.)\s+/, '').replace(/\*\*/g, '');
            if (bullet) {
                if (!$list) { $list = $('<ul class="mb-2"/>'); $wrap.append($list); }
                $list.append($('<li/>').text(clean));
            } else {
                $list = null;
                $wrap.append($('<p class="mb-2"/>').text(clean));
            }
        });
        return $wrap;
    }

    /** Fires onChord when Ctrl and Shift are pressed together and released without any other key or click. */
    function bindChord(onChord) {
        var ctrl = false, shift = false, armed = false;
        function reset() { ctrl = shift = armed = false; }
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Control') ctrl = true;
            else if (e.key === 'Shift') shift = true;
            else { armed = false; return; }
            if (ctrl && shift && !e.altKey && !e.metaKey && !e.repeat) armed = true;
        }, true);
        document.addEventListener('keyup', function (e) {
            if (e.key !== 'Control' && e.key !== 'Shift') return;
            if (armed) { armed = false; onChord(); }
            if (e.key === 'Control') ctrl = false; else shift = false;
        }, true);
        document.addEventListener('mousedown', function () { armed = false; }, true);
        window.addEventListener('blur', reset);
    }

    function init(options) {
        var $root = $('#assistantPanel');
        if (!$root.length) return;
        options = options || {};
        var askUrl = $root.attr('data-ask-url'), historyUrl = $root.attr('data-history-url'), clearUrl = $root.attr('data-clear-url');
        var clientId = $root.attr('data-client-id') || '';
        var token = options.token || $root.find('input[name="__RequestVerificationToken"]').val();
        var $log = $('#assistantLog'), $q = $('#assistantQuestion'), $intro = $log.find('.rpd-chat-intro').detach();
        var busy = false, isOpen = false, loaded = false, lastFocus = null, closeTimer = null;
        var reduceMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

        function scrollDown() { $log.scrollTop($log[0].scrollHeight); }

        function bubble(role, $content, extraClass) {
            var $b = $('<div/>').addClass('rpd-chat-msg rpd-chat-' + role + (extraClass ? ' ' + extraClass : ''));
            $b.append($('<div class="visually-hidden"/>').text(role === 'user' ? 'You said:' : 'Assistant said:'));
            $b.append($content);
            $log.append($b);
            scrollDown();
            return $b;
        }

        function assistantBubble(m) {
            var $b = bubble('assistant', m.error ? $('<p class="mb-0"/>').text(m.text) : renderAnswer(m.text), m.offline ? 'is-offline' : (m.error ? 'is-error' : ''));
            if (!m.offline && !m.error && m.model) $b.append($('<div class="rpd-chat-meta"/>').text('AI-generated from Reflect data (' + m.model + '). Use your clinical judgment.'));
            return $b;
        }

        function showIntroIfEmpty() {
            if (!$log.children('.rpd-chat-msg').length) $log.empty().append($intro.clone());
        }

        function loadHistory() {
            if (loaded) return Promise.resolve();
            return AssistantService.history(historyUrl, clientId).then(function (r) {
                loaded = true;
                $log.empty();
                (r.messages || []).forEach(function (m) {
                    if (m.role === 'user') bubble('user', $('<p class="mb-0"/>').text(m.text));
                    else assistantBubble(m);
                });
                if ((r.messages || []).length) $log.prepend($('<div class="rpd-chat-divider"/>').text('Earlier conversation'));
                showIntroIfEmpty();
                scrollDown();
            }, function (err) {
                if (err.status === 401) window.location.reload();
                showIntroIfEmpty();
            });
        }

        function send(question) {
            question = $.trim(question);
            if (!question || busy) return;
            busy = true;
            $log.find('.rpd-chat-intro').remove();
            bubble('user', $('<p class="mb-0"/>').text(question));
            $q.val('');
            var $thinking = bubble('assistant', $('<p class="mb-0 text-muted"/>').append('<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>').append(document.createTextNode('Reviewing the data...')));

            AssistantService.ask(askUrl, token, clientId, question).then(function (r) {
                $thinking.remove();
                assistantBubble({ text: r.answer, offline: r.offline, error: !r.ok, model: r.model });
            }, function (err) {
                $thinking.remove();
                if (err.status === 401) { window.location.reload(); return; }
                bubble('assistant', $('<p class="mb-0"/>').text(err.error || 'Something went wrong. Try again.'), 'is-error');
            }).then(function () { busy = false; $q.trigger('focus'); });
        }

        function open() {
            if (isOpen) return;
            isOpen = true;
            window.clearTimeout(closeTimer);
            lastFocus = document.activeElement;
            $root.prop('hidden', false).removeClass('is-closing');
            if (!reduceMotion) { $root.removeClass('is-waving'); void $root[0].offsetWidth; $root.addClass('is-waving'); }
            window.requestAnimationFrame(function () { $root.addClass('is-open'); });
            $('body').addClass('rpd-ai-lock');
            loadHistory().then(function () { $q.trigger('focus'); scrollDown(); });
            $q.trigger('focus');
        }

        function close() {
            if (!isOpen) return;
            isOpen = false;
            $root.removeClass('is-open').addClass('is-closing');
            $('body').removeClass('rpd-ai-lock');
            closeTimer = window.setTimeout(function () { $root.prop('hidden', true).removeClass('is-closing is-waving'); }, reduceMotion ? 0 : 260);
            if (lastFocus && lastFocus.focus) lastFocus.focus();
        }

        function toggle() { if (isOpen) close(); else open(); }

        bindChord(toggle);
        $root.on('click', '.js-ai-close', close);
        $root.on('keydown', function (e) {
            if (e.key === 'Escape') { e.preventDefault(); close(); return; }
            if (e.key !== 'Tab') return;
            var $f = $root.find('.rpd-ai-dialog').find('button, textarea, a[href], [tabindex]:not([tabindex="-1"])').filter(':visible:not(:disabled)');
            if (!$f.length) return;
            var first = $f[0], last = $f[$f.length - 1];
            if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
            else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
        });

        $('#assistantForm').on('submit', function (e) { e.preventDefault(); send($q.val()); });
        $q.on('keydown', function (e) { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); send($q.val()); } });
        $root.on('click', '.js-assistant-suggestion', function () { send($(this).text()); });
        $root.on('click', '.js-ai-clear', function () {
            if (!$log.children('.rpd-chat-msg').length) return;
            MessageService.showConfirmationDialogDelete('Clear this conversation?', 'The saved chat will be deleted for you. This cannot be undone.', 'Clear chat', 'Cancel', function () {
                AssistantService.clear(clearUrl, token, clientId).then(function () {
                    $log.empty();
                    showIntroIfEmpty();
                    $q.trigger('focus');
                });
            });
        });

        return { open: open, close: close, toggle: toggle };
    }

    return { init: init };
})(jQuery);
