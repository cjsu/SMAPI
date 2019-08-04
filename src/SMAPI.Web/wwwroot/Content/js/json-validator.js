/* globals $ */

var smapi = smapi || {};
smapi.jsonValidator = function (sectionUrl, pasteID) {
    /**
     * Rebuild the syntax-highlighted element.
     */
    var formatCode = function () {
        Sunlight.highlightAll();
    };

    /**
     * Initialise the JSON validator page.
     */
    var init = function () {
        // code formatting
        formatCode();

        // change format
        $("#output #format").on("change", function() {
            var schemaName = $(this).val();
            location.href = new URL(schemaName + "/" + pasteID, sectionUrl).toString();
        });

        // upload form
        var input = $("#input");
        if (input.length) {
            // disable submit if it's empty
            var toggleSubmit = function () {
                var hasText = !!input.val().trim();
                submit.prop("disabled", !hasText);
            };
            input.on("input", toggleSubmit);
            toggleSubmit();

            // drag & drop file
            input.on({
                'dragover dragenter': function (e) {
                    e.preventDefault();
                    e.stopPropagation();
                },
                'drop': function (e) {
                    var dataTransfer = e.originalEvent.dataTransfer;
                    if (dataTransfer && dataTransfer.files.length) {
                        e.preventDefault();
                        e.stopPropagation();
                        var file = dataTransfer.files[0];
                        var reader = new FileReader();
                        reader.onload = $.proxy(function (file, $input, event) {
                            $input.val(event.target.result);
                            toggleSubmit();
                        }, this, file, $("#input"));
                        reader.readAsText(file);
                    }
                }
            });
        }
    };
    init();
};
