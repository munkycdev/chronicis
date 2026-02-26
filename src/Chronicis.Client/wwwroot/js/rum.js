/**
 * Chronicis DataDog RUM helpers.
 *
 * Provides a safe wrapper around the DD_RUM API so that Blazor components can
 * update the RUM session context with real build-version information at runtime
 * (replacing the placeholder set in index.html before the WASM bundle loads).
 *
 * Usage from .NET JS interop:
 *   await JSRuntime.InvokeVoidAsync("chronicisRum.setVersion", version, sha);
 */
window.chronicisRum = {
    /**
     * Update the DataDog RUM global context with the supplied version strings.
     * Safe to call even when DD_RUM has not been initialised (no-op in that case).
     *
     * @param {string} version  - Full semver build string, e.g. "3.0.142"
     * @param {string} sha      - Short git SHA, e.g. "abc1234"
     */
    setVersion: function (version, sha) {
        if (window.DD_RUM && typeof window.DD_RUM.setGlobalContextProperty === 'function') {
            window.DD_RUM.setGlobalContextProperty('version', version);
            window.DD_RUM.setGlobalContextProperty('build_sha', sha);
        }
    }
};
