$(document).ready(function () {
    /* enable pufferchick */
    var pufferchick = $("#pufferchick");
    $(".cta-dropdown").hover(
        function () {
            pufferchick.attr("src", "Content/images/pufferchick-cool.png");
        },
        function () {
            pufferchick.attr("src", "Content/images/pufferchick.png");
        }
    );

    /* enable download dropdowns */
    $(".cta-dropdown a.download").each(function(i, button) {
        button = $(button);
        var wrapper = button.parent(".cta-dropdown");
        var button = wrapper.find(".download");
        var dropdownContent = wrapper.find(".dropdown-content");

        $(window).on("click", function(e) {
            var target = $(e.target);

            // toggle dropdown on button click
            if (target.is(button) || $.contains(button.get(0), target.get(0))) {
                e.preventDefault();
                dropdownContent.toggle();
            }

            // else hide dropdown
            else
                dropdownContent.hide();
        });
    });
});
