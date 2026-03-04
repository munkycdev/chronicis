window.chronicisMapsDropGuard = window.chronicisMapsDropGuard || (function () {
    let onDragOver = null;
    let onDrop = null;
    let enabled = false;
    let selector = ".maps-basemap-dropzone";

    function hasFiles(event) {
        if (!event || !event.dataTransfer || !event.dataTransfer.types) {
            return false;
        }

        return Array.from(event.dataTransfer.types).some(function (type) {
            return String(type).toLowerCase() === "files";
        });
    }

    function isInsideDropzone(target) {
        return !!(target && typeof target.closest === "function" && target.closest(selector));
    }

    function enable(dropzoneSelector) {
        disable();

        selector = dropzoneSelector || ".maps-basemap-dropzone";

        onDragOver = function (event) {
            if (!hasFiles(event)) {
                return;
            }

            if (isInsideDropzone(event.target)) {
                event.dataTransfer.dropEffect = "copy";
                return;
            }

            event.preventDefault();
            event.stopPropagation();
            event.dataTransfer.dropEffect = "none";
        };

        onDrop = function (event) {
            if (!hasFiles(event)) {
                return;
            }

            if (isInsideDropzone(event.target)) {
                return;
            }

            event.preventDefault();
            event.stopPropagation();
        };

        window.addEventListener("dragover", onDragOver, true);
        window.addEventListener("drop", onDrop, true);
        enabled = true;
    }

    function disable() {
        if (!enabled) {
            return;
        }

        if (onDragOver) {
            window.removeEventListener("dragover", onDragOver, true);
        }

        if (onDrop) {
            window.removeEventListener("drop", onDrop, true);
        }

        onDragOver = null;
        onDrop = null;
        enabled = false;
    }

    return {
        enable: enable,
        disable: disable
    };
})();
