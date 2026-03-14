// ============================================
// CHRONICIS WIKI LINK EXTENSION FOR TIPTAP
// ============================================
// Creates an inline node for wiki-style [[links]]
// ============================================

/**
 * Creates a TipTap Node extension for wiki links
 * Must be called AFTER TipTap is loaded (window.TipTap.Node available)
 * @returns {Object} TipTap Node extension
 */
function createWikiLinkExtension() {
    if (!window.TipTap || !window.TipTap.Node) {
        console.error('TipTap Node not available - cannot create wiki link extension');
        return null;
    }

    return window.TipTap.Node.create({
        name: 'wikiLink',

        group: 'inline',

        inline: true,

        atom: true,

        addAttributes() {
            return {
                targetArticleId: {
                    default: null,
                    parseHTML: element => element.getAttribute('data-target-id'),
                    renderHTML: attributes => {
                        return {
                            'data-target-id': attributes.targetArticleId
                        };
                    }
                },
                displayText: {
                    default: null,
                    parseHTML: element => element.getAttribute('data-display'),
                    renderHTML: attributes => {
                        if (attributes.displayText) {
                            return {
                                'data-display': attributes.displayText
                            };
                        }
                        return {};
                    }
                },
                broken: {
                    default: false,
                    parseHTML: element => element.getAttribute('data-broken') === 'true',
                    renderHTML: attributes => {
                        if (attributes.broken) {
                            return {
                                'data-broken': 'true'
                            };
                        }
                        return {};
                    }
                },
                mapId: {
                    default: null,
                    parseHTML: element => element.getAttribute('data-map-id'),
                    renderHTML: attributes => {
                        if (attributes.mapId) {
                            return {
                                'data-map-id': attributes.mapId
                            };
                        }
                        return {};
                    }
                },
                mapName: {
                    default: null,
                    parseHTML: element => element.getAttribute('data-map-name'),
                    renderHTML: attributes => {
                        if (attributes.mapName) {
                            return {
                                'data-map-name': attributes.mapName
                            };
                        }
                        return {};
                    }
                }
            };
        },

        parseHTML() {
            return [
                {
                    tag: 'span[data-type="wiki-link"]'
                }
            ];
        },

        renderHTML({ node, HTMLAttributes }) {
            if (node.attrs.mapId) {
                const mapName = node.attrs.mapName || node.attrs.displayText || 'Map';

                return [
                    'span',
                    {
                        'data-type': 'wiki-link',
                        class: 'wiki-link-node map-link-node',
                        ...HTMLAttributes
                    },
                    ['span', { class: 'map-link-badge' }, 'MAP'],
                    ['span', { class: 'map-link-text' }, mapName],
                    ['i', { class: 'map-link-icon fa-solid fa-map', 'aria-hidden': 'true' }]
                ];
            }

            const displayText = node.attrs.displayText || 'Loading...';
            
            return [
                'span',
                {
                    'data-type': 'wiki-link',
                    class: 'wiki-link-node',
                    ...HTMLAttributes
                },
                displayText
            ];
        }
    });
}

/**
 * Creates a TipTap Node extension for external links
 * @returns {Object} TipTap Node extension
 */
function createExternalLinkExtension() {
    if (!window.TipTap || !window.TipTap.Node) {
        console.error('TipTap Node not available - cannot create external link extension');
        return null;
    }

    return window.TipTap.Node.create({
        name: 'externalLink',

        group: 'inline',

        inline: true,

        atom: true,

        addAttributes() {
            return {
                source: {
                    default: null,
                    parseHTML: element => element.getAttribute('data-source'),
                    renderHTML: attributes => ({
                        'data-source': attributes.source
                    })
                },
                externalId: {
                    default: null,
                    parseHTML: element => element.getAttribute('data-id'),
                    renderHTML: attributes => ({
                        'data-id': attributes.externalId
                    })
                },
                title: {
                    default: null,
                    parseHTML: element => element.getAttribute('data-title'),
                    renderHTML: attributes => ({
                        'data-title': attributes.title
                    })
                }
            };
        },

        parseHTML() {
            return [
                {
                    tag: 'span[data-type="external-link"]'
                }
            ];
        },

        renderHTML({ node, HTMLAttributes }) {
            const title = node.attrs.title || 'External Link';
            const source = node.attrs.source || '';
            const sourceLabel = source ? source.toUpperCase() : 'EXT';

            return [
                'span',
                {
                    'data-type': 'external-link',
                    class: 'external-link-node',
                    ...HTMLAttributes
                },
                ['span', { class: 'external-link-badge' }, sourceLabel],
                ['span', { class: 'external-link-text' }, title],
                ['i', { class: 'external-link-icon fa-solid fa-arrow-up-right-from-square', 'aria-hidden': 'true' }]
            ];
        }
    });
}

function createMapFeatureLinkExtension() {
    if (!window.TipTap || !window.TipTap.Node) {
        console.error('TipTap Node not available - cannot create map feature link extension');
        return null;
    }

    return window.TipTap.Node.create({
        name: 'mapFeatureLink',
        group: 'inline',
        inline: true,
        atom: true,

        addAttributes() {
            return {
                featureId: {
                    default: null,
                    parseHTML: element => element.getAttribute('data-feature-id'),
                    renderHTML: attributes => ({ 'data-feature-id': attributes.featureId })
                },
                mapId: {
                    default: null,
                    parseHTML: element => element.getAttribute('data-map-id'),
                    renderHTML: attributes => ({ 'data-map-id': attributes.mapId })
                },
                displayText: {
                    default: null,
                    parseHTML: element => element.getAttribute('data-display'),
                    renderHTML: attributes => ({ 'data-display': attributes.displayText })
                },
                mapName: {
                    default: null,
                    parseHTML: element => element.getAttribute('data-map-name'),
                    renderHTML: attributes => ({ 'data-map-name': attributes.mapName })
                }
            };
        },

        parseHTML() {
            return [{ tag: 'span[data-type="map-feature-link"]' }];
        },

        renderHTML({ node, HTMLAttributes }) {
            const displayText = node.attrs.displayText || 'Location';
            const mapName = node.attrs.mapName || 'Map';

            return [
                'span',
                {
                    'data-type': 'map-feature-link',
                    class: 'map-feature-link-node',
                    ...HTMLAttributes
                },
                ['span', { class: 'map-feature-link-badge' }, 'LOC'],
                ['span', { class: 'map-feature-link-text' }, displayText],
                ['span', { class: 'map-feature-link-map' }, mapName]
            ];
        }
    });
}

// Make available globally
window.createWikiLinkExtension = createWikiLinkExtension;
window.createExternalLinkExtension = createExternalLinkExtension;
window.createMapFeatureLinkExtension = createMapFeatureLinkExtension;
