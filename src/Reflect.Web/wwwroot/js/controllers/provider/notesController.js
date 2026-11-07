/*
 * Client notes: the shared "add or edit a note" dialog, the inline form and the list on the Notes tab.
 * Any element with .js-add-note opens the dialog; data-topic, data-subject-id and data-subject-label attach the note
 * to a session, questionnaire, artwork or billing. After any change the page hears 'rpd:notes-changed'.
 */
window.NotesController = (function ($) {
    'use strict';

    function init(options) {
        var $modal = $('#noteModal');
        if (!$modal.length) return;
        options = options || {};
        var clientId = $modal.attr('data-client-id');
        var token = $modal.find('input[name="__RequestVerificationToken"]').val();
        var urls = { save: $modal.attr('data-save-url'), del: $modal.attr('data-delete-url'), pin: $modal.attr('data-pin-url') };
        var modal = bootstrap.Modal.getOrCreateInstance($modal[0]);
        var $body = $('#noteBody'), $err = $modal.find('.js-note-error'), current = null;

        function changed(message) {
            if (message) MessageService.showToastrMessage(message, 'success');
            $(document).trigger('rpd:notes-changed');
        }

        /** Show the new note count on every button for this session, questionnaire or artwork, without reloading the tab. */
        function bumpCount(subjectId) {
            $('.js-add-note[data-subject-id="' + subjectId + '"]').each(function () {
                var $b = $(this), $n = $b.find('span'), n = ($n.length ? parseInt($n.text(), 10) || 0 : 0) + 1;
                if (!$n.length) { $n = $('<span class="ms-1"/>'); $b.append($n); }
                $n.text(n);
                $b.addClass('has-notes').attr('aria-label', ($b.attr('aria-label') || '').replace(/ \(\d+ already\)$/, '') + ' (' + n + ' already)');
            });
        }

        function count() { $modal.find('.js-note-count').text($body.val().length + ' / 2000'); }
        $body.on('input', count);

        function open(note) {
            current = note;
            $err.text('');
            $modal.find('.js-note-title').text(note.id ? 'Edit note' : 'Add a note');
            var about = note.subjectLabel ? 'About: ' + note.topicLabel + ', ' + note.subjectLabel : (note.topicLabel && note.topicLabel !== 'General' ? 'About: ' + note.topicLabel : '');
            $modal.find('.js-note-about').toggleClass('d-none', !about).text(about);
            $body.val(note.body || '');
            $('#notePin').prop('checked', !!note.pinned);
            count();
            modal.show();
        }
        $modal.on('shown.bs.modal', function () { $body.trigger('focus'); });

        $(document).on('click', '.js-add-note', function () {
            var $b = $(this);
            open({ topic: $b.attr('data-topic') || '0', topicLabel: $b.attr('data-topic-label') || '', subjectId: $b.attr('data-subject-id') || '', subjectLabel: $b.attr('data-subject-label') || '' });
        });

        $modal.on('submit', '.js-note-form', function (e) {
            e.preventDefault();
            var text = $.trim($body.val());
            if (!text) { $err.text('Write the note first.'); $body.trigger('focus'); return; }
            var data = { ClientId: clientId, Body: text, IsPinned: $('#notePin').is(':checked'), Topic: current.topic || 0, SubjectId: current.subjectId || '', SubjectLabel: current.subjectLabel || '' };
            if (current.id) data.NoteId = current.id;
            NotesService.save(urls.save, token, data).then(function (r) {
                modal.hide();
                if (!current.id && current.subjectId) bumpCount(current.subjectId);
                changed(r.message);
            },
                function (er) { $err.text(er.error || 'The note could not be saved.'); });
        });

        // Inline form on the Notes tab.
        $(document).on('submit', '.js-note-inline', function (e) {
            e.preventDefault();
            var $f = $(this), $t = $f.find('textarea'), text = $.trim($t.val());
            if (!text) { $t.addClass('is-invalid').trigger('focus'); return; }
            $t.removeClass('is-invalid');
            NotesService.save(urls.save, token, { ClientId: clientId, Body: text, Topic: $f.find('select').val(), IsPinned: $f.find('input[type=checkbox]').is(':checked') })
                .then(function (r) { $t.val(''); changed(r.message); }, function (er) { MessageService.showToastrMessage(er.error || 'The note could not be saved.', 'error'); });
        });

        // List actions (author only).
        $(document).on('click', '.js-note-edit', function () {
            var $li = $(this).closest('[data-note-id]');
            open({ id: $li.attr('data-note-id'), body: $li.find('.js-note-body').text(), pinned: $li.hasClass('is-pinned'), topic: $li.attr('data-topic') });
        });
        $(document).on('click', '.js-note-pin', function () {
            var $li = $(this).closest('[data-note-id]'), pinned = $(this).attr('data-pinned') !== 'true';
            NotesService.pin(urls.pin, token, clientId, $li.attr('data-note-id'), pinned).then(function (r) { changed(r.message); },
                function (er) { MessageService.showToastrMessage(er.error || 'Could not change the pin.', 'error'); });
        });
        $(document).on('click', '.js-note-delete', function () {
            var id = $(this).closest('[data-note-id]').attr('data-note-id');
            MessageService.showConfirmationDialogDelete('Delete this note?', 'It will be removed from this client\'s notes.', 'Delete', 'Cancel', function () {
                NotesService.remove(urls.del, token, clientId, id).then(function (r) { changed(r.message); },
                    function (er) { MessageService.showToastrMessage(er.error || 'The note could not be deleted.', 'error'); });
            });
        });
        $(document).on('click', '.js-note-filter', function () {
            var topic = $(this).attr('data-topic');
            $(this).siblings().addBack().removeClass('active').attr('aria-pressed', 'false');
            $(this).addClass('active').attr('aria-pressed', 'true');
            $(this).closest('.card').find('.rpd-note-item').each(function () { $(this).toggleClass('d-none', !!topic && $(this).attr('data-topic') !== topic); });
        });
    }

    return { init: init };
})(jQuery);
