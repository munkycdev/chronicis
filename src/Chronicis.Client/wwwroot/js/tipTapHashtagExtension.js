// wwwroot/js/tipTapHashtagExtension.js
// Proper TipTap Mark extension for hashtags (Phase 6)

/**
 * Create hashtag mark extension using TipTap's Mark API
 * This properly integrates with TipTap without breaking cursor position
 */
export async function createHashtagExtension() {
    // Import Mark from the CDN
    const { Mark } = await import('https://esm.sh/@tiptap/core@3.11.0');
    const { markInputRule, markPasteRule } = await import('https://esm.sh/@tiptap/core@3.11.0');

    return Mark.create({
        name: 'hashtag',

        priority: 1000,

        // Prevent hashtag mark from being inclusive (don't extend to next characters)
        inclusive: false,

        // Prevent marks from being extended when typing
        exitable: true,

        // How to parse HTML containing hashtags
        parseHTML() {
            return [
                {
                    tag: 'span[data-type="hashtag"]',
                },
            ];
        },

        // How to render hashtags as HTML
        renderHTML({ HTMLAttributes }) {
            return [
                'span',
                {
                    ...HTMLAttributes,
                    'data-type': 'hashtag',
                    'class': 'chronicis-hashtag',
                    'title': 'Hashtag (not yet linked)',
                },
                0, // Content goes here
            ];
        },

        // Define attributes
        addAttributes() {
            return {
                'data-hashtag-name': {
                    default: null,
                    parseHTML: element => element.getAttribute('data-hashtag-name'),
                    renderHTML: attributes => {
                        if (!attributes['data-hashtag-name']) {
                            return {};
                        }
                        return {
                            'data-hashtag-name': attributes['data-hashtag-name'],
                        };
                    },
                },
            };
        },

        // Detect hashtags as you type (triggers when you type space after hashtag)
        addInputRules() {
            return [
                markInputRule({
                    find: /(?:^|\s)(#[a-zA-Z0-9_]+)\s$/,
                    type: this.type,
                    getAttributes: (match) => {
                        return {
                            'data-hashtag-name': match[1].substring(1).toLowerCase(), // Remove # and lowercase
                        };
                    },
                }),
            ];
        },

        // Detect hashtags when pasting
        addPasteRules() {
            return [
                markPasteRule({
                    find: /(?:^|\s)(#[a-zA-Z0-9_]+)(?=\s|$)/g,
                    type: this.type,
                    getAttributes: (match) => {
                        return {
                            'data-hashtag-name': match[1].substring(1).toLowerCase(),
                        };
                    },
                }),
            ];
        },
    });
}

console.log('??? tipTapHashtagExtension.js loaded (TipTap Mark extension)');