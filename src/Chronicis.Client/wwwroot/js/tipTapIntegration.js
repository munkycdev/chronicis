// ================================================
// TipTap Integration
// ================================================

// Global storage for editor instances
window.tipTapEditors = window.tipTapEditors || {};

// Wait for TipTap to be ready
window.addEventListener('tiptap-ready', function() {
    console.log('✅ TipTap ready event received');
});

async function initializeTipTapEditor(editorId, initialContent, dotNetHelper) {
    console.log(`Initializing TipTap editor: ${editorId}`);
    
    // Check if TipTap is loaded
    if (!window.TipTap || !window.TipTap.Editor) {
        console.error('TipTap not loaded yet, waiting...');
        await new Promise(resolve => {
            window.addEventListener('tiptap-ready', resolve, { once: true });
        });
    }

    const container = document.getElementById(editorId);
    if (!container) {
        console.error(`Container not found: ${editorId}`);
        return;
    }

    // Build extensions array
    const extensions = [
        window.TipTap.StarterKit.configure({
            heading: {
                levels: [1, 2, 3, 4, 5, 6]
            },
            bulletList: {
                HTMLAttributes: {
                    class: 'chronicis-bullet-list'
                }
            },
            orderedList: {
                HTMLAttributes: {
                    class: 'chronicis-ordered-list'
                }
            }
        })
    ];

    // Create editor
    const editor = new window.TipTap.Editor({
        element: container,
        extensions: extensions,
        content: initialContent ? markdownToHTML(initialContent) : '<p></p>',
        editable: true,
        onUpdate: ({ editor }) => {
            const html = editor.getHTML();
            const markdown = htmlToMarkdown(html);
            dotNetHelper.invokeMethodAsync('OnEditorUpdate', markdown);
        },
    });

    // Store editor instance
    window.tipTapEditors[editorId] = editor;

    console.log(`✅ TipTap editor created with ID: ${editorId}`);
    return editor;
}

function destroyTipTapEditor(editorId) {
    const editor = window.tipTapEditors[editorId];
    if (editor) {
        editor.destroy();
        delete window.tipTapEditors[editorId];
        console.log(`Editor destroyed: ${editorId}`);
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ================================================
// MARKDOWN <-> HTML CONVERSION
// ================================================

function markdownToHTML(markdown) {
    if (!markdown) return '<p></p>';

    let html = markdown;

    // Headers (# = h1, ## = h2, etc.)
    html = html.replace(/^######\s+(.+)$/gm, '<h6>$1</h6>');
    html = html.replace(/^#####\s+(.+)$/gm, '<h5>$1</h5>');
    html = html.replace(/^####\s+(.+)$/gm, '<h4>$1</h4>');
    html = html.replace(/^###\s+(.+)$/gm, '<h3>$1</h3>');
    html = html.replace(/^##\s+(.+)$/gm, '<h2>$1</h2>');
    html = html.replace(/^#\s+(.+)$/gm, '<h1>$1</h1>');

    // Bold and Italic
    html = html.replace(/\*\*\*(.+?)\*\*\*/g, '<strong><em>$1</em></strong>');
    html = html.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
    html = html.replace(/\*(.+?)\*/g, '<em>$1</em>');

    // Links [text](url)
    html = html.replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2">$1</a>');

    // Code blocks ```
    html = html.replace(/```([\s\S]*?)```/g, '<pre><code>$1</code></pre>');

    // Inline code `
    html = html.replace(/`([^`]+)`/g, '<code>$1</code>');

    // Bullet lists
    html = html.replace(/^[\*\-]\s+(.+)$/gm, '<li>$1</li>');
    html = html.replace(/(<li>.*<\/li>)/s, '<ul class="chronicis-bullet-list">$1</ul>');

    // Ordered lists
    html = html.replace(/^\d+\.\s+(.+)$/gm, '<li>$1</li>');

    // Line breaks
    html = html.replace(/\n\n/g, '</p><p>');
    html = html.replace(/\n/g, '<br>');

    // Wrap in paragraph if not already in a block element
    if (!html.match(/^<(h[1-6]|ul|ol|pre|blockquote|div)/)) {
        html = '<p>' + html + '</p>';
    }

    return html || '<p></p>';
}

function htmlToMarkdown(html) {
    if (!html) return '';

    let markdown = html;

    // Headers
    markdown = markdown.replace(/<h1[^>]*>(.*?)<\/h1>/gi, '# $1\n\n');
    markdown = markdown.replace(/<h2[^>]*>(.*?)<\/h2>/gi, '## $1\n\n');
    markdown = markdown.replace(/<h3[^>]*>(.*?)<\/h3>/gi, '### $1\n\n');
    markdown = markdown.replace(/<h4[^>]*>(.*?)<\/h4>/gi, '#### $1\n\n');
    markdown = markdown.replace(/<h5[^>]*>(.*?)<\/h5>/gi, '##### $1\n\n');
    markdown = markdown.replace(/<h6[^>]*>(.*?)<\/h6>/gi, '###### $1\n\n');

    // Bold and italic
    markdown = markdown.replace(/<strong[^>]*><em[^>]*>(.*?)<\/em><\/strong>/gi, '***$1***');
    markdown = markdown.replace(/<em[^>]*><strong[^>]*>(.*?)<\/strong><\/em>/gi, '***$1***');
    markdown = markdown.replace(/<strong[^>]*>(.*?)<\/strong>/gi, '**$1**');
    markdown = markdown.replace(/<b[^>]*>(.*?)<\/b>/gi, '**$1**');
    markdown = markdown.replace(/<em[^>]*>(.*?)<\/em>/gi, '*$1*');
    markdown = markdown.replace(/<i[^>]*>(.*?)<\/i>/gi, '*$1*');

    // Links
    markdown = markdown.replace(/<a[^>]*href="([^"]*)"[^>]*>(.*?)<\/a>/gi, '[$2]($1)');

    // Code blocks
    markdown = markdown.replace(/<pre[^>]*><code[^>]*>([\s\S]*?)<\/code><\/pre>/gi, '```\n$1\n```\n\n');

    // Inline code
    markdown = markdown.replace(/<code[^>]*>(.*?)<\/code>/gi, '`$1`');

    // Lists
    markdown = markdown.replace(/<ul[^>]*>[\s\S]*?<\/ul>/gi, (match) => {
        return match.replace(/<li[^>]*>(.*?)<\/li>/gi, '* $1\n');
    });

    markdown = markdown.replace(/<ol[^>]*>[\s\S]*?<\/ol>/gi, (match) => {
        let counter = 1;
        return match.replace(/<li[^>]*>(.*?)<\/li>/gi, () => {
            return `${counter++}. $1\n`;
        });
    });

    // Remove remaining HTML tags
    markdown = markdown.replace(/<p[^>]*>/gi, '');
    markdown = markdown.replace(/<\/p>/gi, '\n\n');
    markdown = markdown.replace(/<br\s*\/?>/gi, '\n');
    markdown = markdown.replace(/<[^>]+>/g, '');

    // Clean up extra whitespace
    markdown = markdown.replace(/\n{3,}/g, '\n\n');
    markdown = markdown.trim();

    return markdown;
}

console.log('✅ TipTap integration script loaded');
