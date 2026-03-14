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
        console.error(`Public wiki links container not found: ${containerId}`);
        return;
    }

    if (container.__chronicisPublicWikiLinksHandler) {
        container.removeEventListener('click', container.__chronicisPublicWikiLinksHandler);
    }

    const clickHandler = async (e) => {
        const mapAnchor = e.target.closest('a[href]');
        if (mapAnchor) {
            const mapId = tryGetMapIdFromHref(mapAnchor.getAttribute('href'));
            if (mapId) {
                e.preventDefault();
                e.stopPropagation();

                const mapName = (mapAnchor.textContent || '').trim();
                await dotNetHelper.invokeMethodAsync('OnPublicMapLinkClicked', mapId, mapName);
                return;
            }
        }

        const mapFeatureLink = e.target.closest('span[data-type="map-feature-link"]');
        if (mapFeatureLink) {
            e.preventDefault();
            e.stopPropagation();

            const mapId = mapFeatureLink.getAttribute('data-map-id');
            const featureId = mapFeatureLink.getAttribute('data-feature-id');
            const mapName = mapFeatureLink.getAttribute('data-map-name')
                || mapFeatureLink.querySelector('.map-feature-link-map')?.textContent
                || '';

            if (mapId && featureId) {
                await dotNetHelper.invokeMethodAsync('OnPublicMapFeatureChipClicked', mapId, featureId, mapName);
            }

            return;
        }

        const wikiLink = e.target.closest('span[data-type="wiki-link"]');
        if (wikiLink) {
            e.preventDefault();
            e.stopPropagation();

            const mapId = wikiLink.getAttribute('data-map-id');
            if (mapId) {
                const mapName = wikiLink.getAttribute('data-map-name')
                    || wikiLink.getAttribute('data-display')
                    || wikiLink.textContent
                    || '';
                await dotNetHelper.invokeMethodAsync('OnPublicMapLinkClicked', mapId, mapName);
                return;
            }

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
    };

    container.addEventListener('click', clickHandler);
    container.__chronicisPublicWikiLinksHandler = clickHandler;

    console.debug(`Public wiki links initialized for container: ${containerId}`);
}

/**
 * Cleans up event listeners (optional - for disposal)
 * Note: Since we use event delegation, the listener is on the container
 * and will be cleaned up when the container is removed from DOM.
 */
function disposePublicWikiLinks(containerId) {
    const container = document.getElementById(containerId);
    if (container?.__chronicisPublicWikiLinksHandler) {
        container.removeEventListener('click', container.__chronicisPublicWikiLinksHandler);
        delete container.__chronicisPublicWikiLinksHandler;
    }

    console.debug(`Public wiki links disposed for container: ${containerId}`);
}

function tryGetMapIdFromHref(href) {
    if (!href) {
        return null;
    }

    const match = href.match(/\/maps\/([0-9a-fA-F-]{36})(?:[/?#]|$)/);
    return match ? match[1] : null;
}

// Expose functions globally for Blazor interop
window.initializePublicWikiLinks = initializePublicWikiLinks;
window.disposePublicWikiLinks = disposePublicWikiLinks;
