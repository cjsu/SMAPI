/* globals $ */
var smapi = smapi || {};

/**
 * Implements the logic for a log/JSON file upload form.
 *
 * @param {object} opts The file upload form options.
 * @param {jQuery} opts.chooseFileLink The clickable link which shows the file chooser.
 * @param {jQuery} opts.chooseFileInput The file input element.
 * @param {jQuery} opts.contentArea The file content area.
 * @param {jQuery} opts.submitButton The submit button.
 */
smapi.fileUpload = function (opts) {
    /**
     * Toggle the submit button if the form has content.
     */
    var toggleSubmit = function () {
        var hasText = !!opts.contentArea.val().trim();
        opts.submitButton.prop("disabled", !hasText);
    };

    /**
     * Paste the content of a file into the content area.
     * @param {File} file The file whose content to paste.
     */
    var pasteFile = function (file) {
        var reader = new FileReader();
        reader.onload = $.proxy(function (file, $input, event) {
            $input.val(event.target.result);
            toggleSubmit();
        }, this, file, $("#input"));
        reader.readAsText(file);
    };

    // initialize
    if (opts.contentArea.length) {
        // disable submit button if it's empty
        opts.contentArea.on("input", toggleSubmit);
        toggleSubmit();

        // drag & drop file
        opts.contentArea.on({
            "dragover dragenter": function (e) {
                e.preventDefault();
            },
            "drop": function (e) {
                e.preventDefault();

                var transfer = e.originalEvent.dataTransfer;
                if (transfer && transfer.files.length)
                    pasteFile(transfer.files[0]);
            }
        });

        // choose file
        opts.chooseFileLink.on({
            "click": function (e) {
                e.preventDefault();
                opts.chooseFileInput.click();
            }
        });
        opts.chooseFileInput.on({
            "change": function (e) {
                if (!e.target.files || !e.target.files.length)
                    return;

                pasteFile(e.target.files[0]);
            }
        });
    }
};
