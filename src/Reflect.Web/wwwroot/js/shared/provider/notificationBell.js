/* Header bell: polls the unread count every 60 s (does not extend the session), loads the list when opened. */
(function (window, $) {
    'use strict';
    var POLL_MS = 60000;

    function init() {
        var $bell = $('#notifBell');
        if (!$bell.length) return;
        var $badge = $bell.find('.rpd-bell-badge'), $body = $bell.find('.js-notif-body'), $live = $bell.find('.js-notif-live');
        var lastUrgent = 0, loaded = false;

        function render(c) {
            var n = (c && c.unread) || 0, urgent = (c && c.urgent) || 0;
            $badge.toggleClass('d-none', n === 0).toggleClass('is-urgent', urgent > 0).text(n > 9 ? '9+' : n);
            $bell.find('.rpd-bell').attr('aria-label', n ? 'Notifications, ' + n + ' unread' + (urgent ? ', ' + urgent + ' urgent' : '') : 'Notifications');
            if (loaded && urgent > lastUrgent) {
                MessageService.showToastrMessage(urgent === 1 ? 'New urgent notification.' : urgent + ' urgent notifications.', 'warning');
            }
            lastUrgent = urgent;
            loaded = true;
        }

        function refresh() {
            NotificationService.count($bell.data('count-url')).done(render).fail(function (xhr) {
                if (xhr.status === 401) window.location.reload();
            });
        }

        $bell.on('show.bs.dropdown', function () {
            NotificationService.list($bell.data('list-url')).done(function (html) {
                $body.html(html);
                $live.text($body.find('li').length ? $body.find('li').length + ' notifications' : 'No notifications');
            });
        });

        $bell.on('click', '.js-notif-read-all', function () {
            var token = $bell.find('.js-notif-token input[name="__RequestVerificationToken"]').val();
            NotificationService.markAllRead($bell.data('read-url'), token).done(function (c) {
                render(c);
                $body.find('li').addClass('is-read');
                $body.find('.rpd-notif-dot').remove();
                $live.text('All notifications marked as read');
            });
        });

        refresh();
        window.setInterval(function () { if (!document.hidden) refresh(); }, POLL_MS);
        $(document).on('rpd:livedata', refresh);
    }

    $(init);
})(window, jQuery);
