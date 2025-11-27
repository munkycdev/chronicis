// ================================================
// TipTap Hashtag Extension - Phase 7.3
// ================================================

export async function createHashtagExtension() {
    const { Mark } = await import('https://esm.sh/@tiptap/core@3.11.0');
    const { markInputRule, markPasteRule } = await import('https://esm.sh/@tiptap/core@3.11.0');

    return Mark.create({
        name: 'hashtag',
        priority: 1000,
        inclusive: false,
        exitable: true,

        parseHTML() {
            return [{ tag: 'span[data-type="hashtag"]' }];
        },

        renderHTML({ HTMLAttributes }) {
            return [
                'span',
                {
                    ...HTMLAttributes,
                    'data-type': 'hashtag',
                    'class': 'chronicis-hashtag',
                    'title': 'Click to navigate or link',
                },
                0,
            ];
        },

        addAttributes() {
            return {
                'data-hashtag-name': {
                    default: null,
                    parseHTML: element => element.getAttribute('data-hashtag-name'),
                    renderHTML: attributes => {
                        if (!attributes['data-hashtag-name']) return {};
                        return { 'data-hashtag-name': attributes['data-hashtag-name'] };
                    },
                },
                'data-linked': {
                    default: 'false',
                    parseHTML: element => element.getAttribute('data-linked') || 'false',
                    renderHTML: attributes => {
                        return { 'data-linked': attributes['data-linked'] || 'false' };
                    },
                },
                'data-article-slug': {
                    default: null,
                    parseHTML: element => element.getAttribute('data-article-slug'),
                    renderHTML: attributes => {
                        if (!attributes['data-article-slug']) return {};
                        return { 'data-article-slug': attributes['data-article-slug'] };
                    },
                },
                'data-article-id': {
                    default: null,
                    parseHTML: element => element.getAttribute('data-article-id'),
                    renderHTML: attributes => {
                        if (!attributes['data-article-id']) return {};
                        return { 'data-article-id': attributes['data-article-id'] };
                    },
                },
            };
        },

        addInputRules() {
            return [
                markInputRule({
                    find: /(?:^|\s)(#[a-zA-Z0-9_]+)\s$/,
                    type: this.type,
                    getAttributes: (match) => {
                        return {
                            'data-hashtag-name': match[1].substring(1).toLowerCase(),
                            'data-linked': 'false',
                        };
                    },
                }),
            ];
        },

        addPasteRules() {
            return [
                markPasteRule({
                    find: /(?:^|\s)(#[a-zA-Z0-9_]+)(?=\s|$)/g,
                    type: this.type,
                    getAttributes: (match) => {
                        return {
                            'data-hashtag-name': match[1].substring(1).toLowerCase(),
                            'data-linked': 'false',
                        };
                    },
                }),
            ];
        },
    });
}

console.log('✅ Hashtag extension module loaded (Phase 7.3)');
