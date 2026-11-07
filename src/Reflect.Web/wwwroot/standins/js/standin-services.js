/* STAND-IN: rebuilds of the Reflect XR global JS services with the same names and behavior
   (tech spec Part 2, "Shared JS services"). Delete on merge; the real ones take over. */
(function (window, $) {
    'use strict';

    // ---------- MessageService ----------
    var MessageService = {
        showToastrMessage: function (message, messageType, timeOut) {
            if (!message) return;
            var type = (messageType || 'info').toLowerCase();
            if (type === 'danger') type = 'error';
            if (['success', 'error', 'warning', 'info'].indexOf(type) < 0) type = 'info';
            toastr.options = { positionClass: 'toast-top-right', closeButton: true, progressBar: true, timeOut: timeOut === undefined ? 3000 : timeOut };
            toastr[type](message);
        },
        _confirm: function (icon, confirmColor, title, message, yesButtonText, noButtonText, onConfirm, onCancel) {
            Swal.fire({
                title: title, html: message, icon: icon, showCancelButton: true,
                confirmButtonText: yesButtonText || 'Yes', cancelButtonText: noButtonText || 'Cancel',
                confirmButtonColor: confirmColor, reverseButtons: true, focusCancel: icon === 'warning'
            }).then(function (result) {
                if (result.isConfirmed) { if (onConfirm) onConfirm(); }
                else if (onCancel) { onCancel(); }
            });
        },
        showConfirmationDialogSuccess: function (title, message, yes, no, onConfirm, onCancel) { MessageService._confirm('question', '#1F305E', title, message, yes, no, onConfirm, onCancel); },
        showConfirmationDialogDelete: function (title, message, yes, no, onConfirm, onCancel) { MessageService._confirm('warning', '#d9534f', title, message, yes, no, onConfirm, onCancel); },
        showConfirmationDialogInfo: function (title, message, yes, no, onConfirm, onCancel) { MessageService._confirm('info', '#586F8A', title, message, yes, no, onConfirm, onCancel); },
        cancelAction: function () { },
        clearAllMessages: function () { toastr.clear(); }
    };
    window.MessageService = MessageService;

    // ---------- Loading overlay ----------
    window.displayLoading = function (msg) {
        var $o = $('#standinLoading');
        $o.find('.standin-loading-text').text(msg || 'Loading...');
        $o.addClass('show').attr('aria-hidden', 'false');
    };
    window.hideDisplayLoading = function () { $('#standinLoading').removeClass('show').attr('aria-hidden', 'true'); };

    // ---------- renderBadge ----------
    window.renderBadge = function (color, text, iconClass) {
        var $s = $('<span/>').addClass('badge bg-' + color + '-transparent text-' + color);
        if (iconClass) $s.append($('<i/>').addClass(iconClass + ' me-1').attr('aria-hidden', 'true'));
        $s.append(document.createTextNode(text));
        return $s.prop('outerHTML');
    };

    // ---------- DataTables helpers ----------
    window.initTabDataTableFix = function (tabsContainerId) {
        $(tabsContainerId).on('shown.bs.tab', 'button[data-bs-toggle="tab"], a[data-bs-toggle="tab"]', function () {
            $.fn.dataTable.tables({ visible: true, api: true }).columns.adjust().responsive.recalc();
        });
    };

    window.initTabPersistence = function (containerId) {
        var key = 'tab:' + window.location.pathname + ':' + containerId;
        var $c = $('#' + String(containerId).replace(/^#/, ''));
        var hash = window.location.hash;
        var target = hash && $c.find('[data-bs-target="' + hash + '"]').length ? hash : null;
        if (!target) { try { target = window.localStorage.getItem(key); } catch (e) { target = null; } }
        if (target) {
            var el = $c.find('[data-bs-target="' + target + '"]')[0];
            if (el) bootstrap.Tab.getOrCreateInstance(el).show();
        }
        $c.on('shown.bs.tab', '[data-bs-toggle="tab"]', function (e) {
            try { window.localStorage.setItem(key, $(e.target).attr('data-bs-target')); } catch (err) { /* storage unavailable */ }
        });
    };

    $(function () {
        // Client-side DataTables
        $('table.dt-client').each(function () {
            var $t = $(this);
            var lastHeader = $.trim($t.find('thead th').last().text());
            var opts = { responsive: true, autoWidth: false, language: { search: '', searchPlaceholder: 'Search...' } };
            if (lastHeader === 'Actions') opts.columnDefs = [{ targets: -1, orderable: false }];
            $t.DataTable(opts);
        });

        // Required-field stars
        $('input[required], select[required], textarea[required], [data-val-required]').each(function () {
            var id = $(this).attr('id');
            if (!id) return;
            var $label = $('label[for="' + id + '"]');
            if ($label.length && !$label.find('.required-star').length && !$(this).is(':checkbox')) {
                $label.append(' <span class="required-star text-danger" aria-hidden="true">*</span>');
            }
        });

        // Confirm-before-submit
        $(document).on('click', '.js-confirm-submit', function (e) {
            e.preventDefault();
            var $btn = $(this), form = $btn.closest('form')[0];
            var style = ($btn.data('confirm-style') || 'delete').toLowerCase();
            var fn = style === 'success' ? MessageService.showConfirmationDialogSuccess
                : style === 'info' ? MessageService.showConfirmationDialogInfo
                    : MessageService.showConfirmationDialogDelete;
            var text = $('<div/>').text($btn.data('confirm-text') || '').html();
            fn($btn.data('confirm-title') || 'Are you sure?', text, $btn.data('confirm-label') || 'Yes', 'Cancel', function () {
                if (form.requestSubmit) form.requestSubmit(); else form.submit();
            }, MessageService.cancelAction);
        });

        // Declarative behaviors
        $(document).on('click', '.card-clickable[data-href]', function () { window.location.href = $(this).data('href'); });
        $(document).on('change', '.js-auto-submit-select', function () { $(this).closest('form').trigger('submit'); });
        $(document).on('click', '.js-stop-propagation', function (e) { e.stopPropagation(); });
    });
})(window, jQuery);
