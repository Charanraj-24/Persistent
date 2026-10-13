/* ==========================================================================
   STAND-IN SHARED SERVICES (delete on merge)
   Same names and behavior as the global services the real Reflect XR app loads
   on every page (tech spec, Part 2 "Shared JS services").
   ========================================================================== */
(function (window, $) {
    'use strict';

    var MessageService = {
        showToastrMessage: function (message, messageType, timeOut) {
            if (!message) return;
            var type = (messageType || 'info').toLowerCase();
            if (type === 'danger') type = 'error';
            if (['success', 'error', 'warning', 'info'].indexOf(type) < 0) type = 'info';
            toastr.options = {
                positionClass: 'toast-top-right',
                closeButton: true,
                progressBar: true,
                newestOnTop: true,
                timeOut: timeOut === undefined ? 3000 : timeOut
            };
            toastr[type](message);
        },

        showConfirmationDialogSuccess: function (title, message, yesButtonText, noButtonText, onConfirm, onCancel) {
            confirmDialog('success', title, message, yesButtonText, noButtonText, onConfirm, onCancel);
        },

        showConfirmationDialogDelete: function (title, message, yesButtonText, noButtonText, onConfirm, onCancel) {
            confirmDialog('warning', title, message, yesButtonText, noButtonText, onConfirm, onCancel);
        },

        showConfirmationDialogInfo: function (title, message, yesButtonText, noButtonText, onConfirm, onCancel) {
            confirmDialog('info', title, message, yesButtonText, noButtonText, onConfirm, onCancel);
        },

        cancelAction: function () { },

        clearAllMessages: function () {
            toastr.clear();
        }
    };

    function confirmDialog(icon, title, message, yesText, noText, onConfirm, onCancel) {
        Swal.fire({
            icon: icon,
            title: title,
            html: message,
            showCancelButton: true,
            confirmButtonText: yesText || 'Yes',
            cancelButtonText: noText || 'Cancel',
            reverseButtons: true,
            focusCancel: icon === 'warning'
        }).then(function (result) {
            if (result.isConfirmed) {
                if (typeof onConfirm === 'function') onConfirm();
            } else if (typeof onCancel === 'function') {
                onCancel();
            }
        });
    }

    window.MessageService = MessageService;

    window.displayLoading = function (msg) {
        var $overlay = $('#loadingOverlay');
        $overlay.find('.loading-text').text(msg || 'Loading...');
        $overlay.addClass('show').attr('aria-hidden', 'false');
    };

    window.hideDisplayLoading = function () {
        $('#loadingOverlay').removeClass('show').attr('aria-hidden', 'true');
    };

    window.renderBadge = function (color, text, iconClass) {
        var safe = $('<div>').text(text == null ? '' : String(text)).html();
        var icon = iconClass ? '<i class="' + iconClass + ' me-1" aria-hidden="true"></i>' : '';
        return '<span class="badge bg-' + color + '-transparent text-' + color + '">' + icon + safe + '</span>';
    };

    window.initTabDataTableFix = function (tabsContainerSelector) {
        $(tabsContainerSelector).on('shown.bs.tab', 'button[data-bs-toggle="tab"], a[data-bs-toggle="tab"]', function (e) {
            var target = $(e.target).attr('data-bs-target') || $(e.target).attr('href');
            $(target).find('table.dataTable').each(function () {
                $(this).DataTable().columns.adjust().responsive.recalc();
            });
        });
    };

    window.initTabPersistence = function (containerId) {
        var id = String(containerId).replace(/^#/, '');
        var key = 'tab:' + window.location.pathname + ':' + id;
        var $container = $('#' + id);
        try {
            var saved = window.localStorage.getItem(key);
            if (saved) {
                var trigger = $container.find('[data-bs-target="' + saved + '"]').get(0);
                if (trigger) bootstrap.Tab.getOrCreateInstance(trigger).show();
            }
        } catch (err) { /* storage unavailable */ }
        $container.on('shown.bs.tab', '[data-bs-toggle="tab"]', function (e) {
            try { window.localStorage.setItem(key, $(e.target).attr('data-bs-target')); } catch (err) { /* ignore */ }
        });
    };

    $(function () {
        // Required-field stars
        $('input, select, textarea').filter('[required], [data-val-required]').each(function () {
            var id = $(this).attr('id');
            if (!id) return;
            var $label = $('label[for="' + id + '"]');
            if ($label.length && !$label.find('.required-star').length) {
                $label.append('<span class="required-star" aria-hidden="true">*</span>');
            }
        });

        // Client-side DataTables
        $('table.dt-client').each(function () {
            var $table = $(this);
            var lastHeader = $.trim($table.find('thead th').last().text());
            var pageLength = parseInt($table.data('page-length'), 10) || 10;
            var lengths = [pageLength, 10, 25, 50, 100].filter(function (v, i, a) { return a.indexOf(v) === i; }).sort(function (a, b) { return a - b; });
            var options = {
                responsive: true,
                pageLength: pageLength,
                lengthMenu: lengths,
                order: $table.data('order') || [],
                language: { search: '', searchPlaceholder: 'Search...', lengthMenu: 'Show _MENU_' }
            };
            if (lastHeader === 'Actions') {
                options.columnDefs = [{ targets: -1, orderable: false }];
            }
            $table.DataTable(options);
        });

        // Confirm-before-submit
        $(document).on('click', '.js-confirm-submit', function (e) {
            e.preventDefault();
            var $btn = $(this);
            var form = $btn.closest('form').get(0);
            var style = ($btn.data('confirm-style') || 'delete').toLowerCase();
            var fn = style === 'success' ? MessageService.showConfirmationDialogSuccess
                : style === 'info' ? MessageService.showConfirmationDialogInfo
                    : MessageService.showConfirmationDialogDelete;
            fn($btn.data('confirm-title') || 'Are you sure?', $btn.data('confirm-text') || '', $btn.data('confirm-label') || 'Yes', 'Cancel',
                function () { if (form) form.submit(); }, MessageService.cancelAction);
        });

        // Small declarative behaviors
        $(document).on('click', '.card-clickable[data-href]', function () {
            window.location.href = $(this).data('href');
        });
        $(document).on('keydown', '.card-clickable[data-href]', function (e) {
            if (e.key === 'Enter') window.location.href = $(this).data('href');
        });
        $(document).on('change', '.js-auto-submit-select', function () {
            $(this).closest('form').trigger('submit');
        });
        $(document).on('click', '.js-stop-propagation', function (e) {
            e.stopPropagation();
        });
    });
})(window, jQuery);
