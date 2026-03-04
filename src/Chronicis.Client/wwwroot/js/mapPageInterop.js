export function getElementRect(el) {
    if (!el) {
        return null;
    }

    const rect = el.getBoundingClientRect();
    return {
        left: rect.left,
        top: rect.top,
        width: rect.width,
        height: rect.height,
        Left: rect.left,
        Top: rect.top,
        Width: rect.width,
        Height: rect.height
    };
}

window.chronicisMapPage = window.chronicisMapPage || {};
window.chronicisMapPage.getElementRect = getElementRect;
