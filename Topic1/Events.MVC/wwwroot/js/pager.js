(function () {
    function validRange(value, min, max) {
        if (!/^\d+$/.test(value)) {
            return false;
        }

        var page = parseInt(value, 10);
        return page >= min && page <= max;
    }

    function goToPage(input) {
        var value = input.value.trim();
        var min = parseInt(input.dataset.min, 10);
        var max = parseInt(input.dataset.max, 10);

        if (!validRange(value, min, max)) {
            input.value = input.dataset.current || "";
            return;
        }

        var url = (input.dataset.urlTemplate || "").replace("__page__", value);
        if (!url) {
            return;
        }

        var target = input.dataset.target;
        var swap = input.dataset.swap || "outerHTML";
        var pushUrl = input.dataset.pushUrl === "true";

        if (window.htmx && target) {
            htmx.ajax("GET", url, {
                target: target,
                swap: swap,
                pushUrl: pushUrl
            });
            return;
        }

        window.location.href = url;
    }

    document.addEventListener("focusin", function (event) {
        if (event.target.matches(".pagebox")) {
            event.target.select();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (!event.target.matches(".pagebox")) {
            return;
        }

        if (event.key === "Enter") {
            event.preventDefault();
            goToPage(event.target);
        }
        else if (event.key === "Escape") {
            event.target.value = event.target.dataset.current || "";
        }
    });
})();
