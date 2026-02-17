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

// Make available globally
window.createWikiLinkExtension = createWikiLinkExtension;
window.createExternalLinkExtension = createExternalLinkExtension;
