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

const _wheelHandlers = new WeakMap();

export function setupWheelInterceptor(el, dotNetRef) {
    if (!el) return;

    // Remove any existing handler first
    teardownWheelInterceptor(el);

    const handler = (e) => {
        // Intercept pinch-to-zoom (ctrlKey=true on trackpad pinch and Ctrl+scroll)
        if (!e.ctrlKey) return;
        e.preventDefault();
        e.stopPropagation();
        dotNetRef.invokeMethodAsync('OnWheelZoom', e.deltaY);
    };

    el.addEventListener('wheel', handler, { passive: false });
    _wheelHandlers.set(el, handler);
}

export function teardownWheelInterceptor(el) {
    if (!el) return;
    const handler = _wheelHandlers.get(el);
    if (handler) {
        el.removeEventListener('wheel', handler);
        _wheelHandlers.delete(el);
    }
}

window.chronicisMapPage = window.chronicisMapPage || {};
window.chronicisMapPage.getElementRect = getElementRect;
