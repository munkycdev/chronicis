// wwwroot/js/tipTapHashtagExtension.js
// TipTap extension for rendering hashtags with visual styling

/**
 * Hashtag TipTap Extension
 * Detects #word patterns and renders them as styled nodes
 */
export function createHashtagExtension() {
    const { Mark } = window.TipTap;

    return Mark.create({
        name: 'hashtag',

        // Define the schema for how hashtags are represented
        parseHTML() {
            return [
                {
                    tag: 'span[data-hashtag]',
                },
            ];
        },

        renderHTML({ HTMLAttributes }) {
            return ['span', {
                ...HTMLAttributes,
                'data-hashtag': '',
                'class': 'chronicis-hashtag',
                'title': 'Hashtag (not yet linked)' // Phase 6: no linking yet
            }, 0];
        },

        // Define attributes that can be stored
        addAttributes() {
            return {
                hashtagName: {
                    default: null,
                    parseHTML: element => element.getAttribute('data-hashtag-name'),
                    renderHTML: attributes => {
                        return {
                            'data-hashtag-name': attributes.hashtagName,
                        };
                    },
                },
            };
        },

        // Parse hashtags from plain text input
        addInputRules() {
            const { markInputRule } = window.TipTap;
            
            return [
                // Match #word pattern as user types
                markInputRule({
                    find: /#(\w+)/g,
                    type: this.type,
                    getAttributes: (match) => {
                        return {
                            hashtagName: match[1].toLowerCase(),
                        };
                    },
                }),
            ];
        },

        // Parse hashtags from pasted content
        addPasteRules() {
            const { markPasteRule } = window.TipTap;
            
            return [
                markPasteRule({
                    find: /#(\w+)/g,
                    type: this.type,
                    getAttributes: (match) => {
                        return {
                            hashtagName: match[1].toLowerCase(),
                        };
                    },
                }),
            ];
        },
    });
}

/**
 * Initialize hashtag extension with the editor
 */
window.initializeHashtagExtension = function(editor) {
    if (!editor || !window.TipTap) {
        console.error('TipTap editor or library not available');
        return;
    }

    // Hashtag extension is already added during editor creation
    // This function is here for future enhancements
    console.log('Hashtag extension initialized');
};
