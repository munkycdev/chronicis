// ============================================
// PUBLIC WIKI LINK HANDLER
// ============================================
// Handles wiki link clicks on public world pages
// where TipTap editor is not initialized
// ============================================

/**
 * Attaches click handlers to wiki link spans within a container.
 * @param {string} containerId - The ID of the container element
 * @param {object} dotNetHelper - The Blazor DotNetObjectReference for callbacks
 */
function initializePublicWikiLinks(containerId, dotNetHelper) {
    const container = document.getElementById(containerId);
    if (!container) {
        _logger.LogError(`Public wiki links container not found: ${containerId}`);
        return;
    }

    // Use event delegation on the container
    container.addEventListener('click', async (e) => {
        const wikiLink = e.target.closest('span[data-type="wiki-link"]');
        if (wikiLink) {
            e.preventDefault();
            e.stopPropagation();

            const targetArticleId = wikiLink.getAttribute('data-target-id');
            const isBroken = wikiLink.getAttribute('data-broken') === 'true';

            if (targetArticleId && !isBroken) {
                try {
                    await dotNetHelper.invokeMethodAsync('OnPublicWikiLinkClicked', targetArticleId);
                } catch (err) {
                    console.error('Error handling wiki link click:', err);
                }
            }
        }
    });

    console.debug(`Public wiki links initialized for container: ${containerId}`);
}

/**
 * Cleans up event listeners (optional - for disposal)
 * Note: Since we use event delegation, the listener is on the container
 * and will be cleaned up when the container is removed from DOM.
 */
function disposePublicWikiLinks(containerId) {
    // Event delegation means we don't need explicit cleanup
    // The container removal handles it automatically
    console.debug(`Public wiki links disposed for container: ${containerId}`);
}

// Expose functions globally for Blazor interop
window.initializePublicWikiLinks = initializePublicWikiLinks;
window.disposePublicWikiLinks = disposePublicWikiLinks;
