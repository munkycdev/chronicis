// tipTapIntegration.js - UPDATED with ready check
// Place this in wwwroot/js/tipTapIntegration.js

// Store editor instances
let editorInstances = {};
let tiptapReady = false;

// Listen for TipTap ready event
window.addEventListener('tiptap-ready', function () {
    tiptapReady = true;
    console.log('📝 tipTapIntegration: TipTap is ready');
});

window.initTipTapEditor = (editorId, initialContent, dotNetHelper) => {
    console.log('🎯 initTipTapEditor called for:', editorId);

    // Check if TipTap is ready
    if (!tiptapReady || typeof window.TipTap === 'undefined') {
        console.log('⏳ TipTap not ready yet, waiting...');

        // Wait for TipTap to be ready
        var readyCheckInterval = setInterval(function () {
            if (window.TipTap && typeof window.TipTap.Editor !== 'undefined') {
                clearInterval(readyCheckInterval);
                console.log('✅ TipTap now ready, initializing editor...');
                createEditor(editorId, initialContent, dotNetHelper);
            }
        }, 100);

        // Safety timeout
        setTimeout(function () {
            clearInterval(readyCheckInterval);
            if (!window.TipTap) {
                console.error('❌ Timeout waiting for TipTap');
            }
        }, 5000);

        return;
    }

    // TipTap is ready, create editor immediately
    createEditor(editorId, initialContent, dotNetHelper);
};

function createEditor(editorId, initialContent, dotNetHelper) {
    const container = document.getElementById(editorId);
    if (!container) {
        console.error('❌ Editor container not found:', editorId);
        return;
    }

    // Destroy existing instance if any
    if (editorInstances[editorId]) {
        console.log('🗑️ Destroying existing editor instance');
        editorInstances[editorId].destroy();
        delete editorInstances[editorId];
    }

    try {
        console.log('🔨 Creating TipTap editor...');
        console.log('   Container:', container);
        console.log('   Initial content length:', initialContent ? initialContent.length : 0);

        // Initialize TipTap
        const editor = new window.TipTap.Editor({
            element: container,
            extensions: [
                window.TipTap.StarterKit.configure({
                    heading: {
                        levels: [1, 2, 3, 4, 5, 6],
                    },
                }),
            ],
            content: initialContent ? markdownToHTML(initialContent) : '<p></p>',
            editorProps: {
                attributes: {
                    class: 'chronicis-editor-content',
                    'data-placeholder': 'Start typing your campaign notes...',
                },
            },
            onCreate: ({ editor }) => {
                // Focus after editor is fully created
                editor.commands.focus('end');
            },
            onUpdate: ({ editor }) => {
                // Convert HTML back to markdown
                const markdown = htmlToMarkdown(editor.getHTML());

                // Notify Blazor
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnEditorUpdate', markdown);
                }
            },
        });

        // Store instance
        editorInstances[editorId] = editor;

        console.log('✅ TipTap editor created successfully!');
    } catch (error) {
        console.error('❌ Error creating TipTap editor:', error);
        console.error('   Stack:', error.stack);
    }
}

window.getTipTapMarkdown = (editorId) => {
    const editor = editorInstances[editorId];
    if (!editor) {
        console.warn('⚠️ No editor found for:', editorId);
        return '';
    }

    return htmlToMarkdown(editor.getHTML());
};

window.setTipTapContent = (editorId, markdown) => {
    const editor = editorInstances[editorId];
    if (!editor) {
        console.warn('⚠️ No editor found for:', editorId);
        return;
    }

    editor.commands.setContent(markdownToHTML(markdown));
};

window.destroyTipTapEditor = (editorId) => {
    const editor = editorInstances[editorId];
    if (editor) {
        console.log('🗑️ Destroying editor:', editorId);
        editor.destroy();
        delete editorInstances[editorId];
    }
};

// Simple markdown to HTML conversion
function markdownToHTML(markdown) {
    if (!markdown) return '<p></p>';

    let html = markdown;

    // Headers (must be done in order from h6 to h1)
    html = html.replace(/^######\s+(.+)$/gm, '<h6>$1</h6>');
    html = html.replace(/^#####\s+(.+)$/gm, '<h5>$1</h5>');
    html = html.replace(/^####\s+(.+)$/gm, '<h4>$1</h4>');
    html = html.replace(/^###\s+(.+)$/gm, '<h3>$1</h3>');
    html = html.replace(/^##\s+(.+)$/gm, '<h2>$1</h2>');
    html = html.replace(/^#\s+(.+)$/gm, '<h1>$1</h1>');

    // Bold
    html = html.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
    html = html.replace(/__(.+?)__/g, '<strong>$1</strong>');

    // Italic
    html = html.replace(/\*(.+?)\*/g, '<em>$1</em>');
    html = html.replace(/_(.+?)_/g, '<em>$1</em>');

    // Inline code
    html = html.replace(/`(.+?)`/g, '<code>$1</code>');

    // Code blocks
    html = html.replace(/```(\w+)?\n([\s\S]+?)```/g, '<pre><code>$2</code></pre>');

    // Links
    html = html.replace(/\[(.+?)\]\((.+?)\)/g, '<a href="$2">$1</a>');

    // Blockquotes
    html = html.replace(/^>\s+(.+)$/gm, '<blockquote><p>$1</p></blockquote>');

    // Unordered lists
    html = html.replace(/^\-\s+(.+)$/gm, '<li>$1</li>');
    html = html.replace(/^\*\s+(.+)$/gm, '<li>$1</li>');

    // Wrap consecutive <li> in <ul>
    html = html.replace(/(<li>.*<\/li>\n?)+/g, '<ul>$&</ul>');

    // Ordered lists
    html = html.replace(/^\d+\.\s+(.+)$/gm, '<li>$1</li>');

    // Horizontal rule
    html = html.replace(/^---$/gm, '<hr>');

    // Paragraphs
    const lines = html.split('\n');
    const processed = lines.map(line => {
        line = line.trim();
        if (!line) return '';
        if (line.match(/^<[^>]+>/)) return line;
        return `<p>${line}</p>`;
    });

    html = processed.join('\n');

    // Clean up
    html = html.replace(/<p><\/p>/g, '');
    html = html.replace(/\n{3,}/g, '\n\n');

    return html || '<p></p>';
}

// Simple HTML to markdown conversion
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

    // Bold
    markdown = markdown.replace(/<strong[^>]*>(.*?)<\/strong>/gi, '**$1**');
    markdown = markdown.replace(/<b[^>]*>(.*?)<\/b>/gi, '**$1**');

    // Italic
    markdown = markdown.replace(/<em[^>]*>(.*?)<\/em>/gi, '*$1*');
    markdown = markdown.replace(/<i[^>]*>(.*?)<\/i>/gi, '*$1*');

    // Code
    markdown = markdown.replace(/<code[^>]*>(.*?)<\/code>/gi, '`$1`');

    // Code blocks
    markdown = markdown.replace(/<pre[^>]*><code[^>]*>([\s\S]*?)<\/code><\/pre>/gi, '```\n$1\n```\n\n');

    // Links
    markdown = markdown.replace(/<a[^>]*href="([^"]*)"[^>]*>(.*?)<\/a>/gi, '[$2]($1)');

    // Blockquotes
    markdown = markdown.replace(/<blockquote[^>]*><p[^>]*>(.*?)<\/p><\/blockquote>/gi, '> $1\n\n');

    // Lists
    markdown = markdown.replace(/<li[^>]*>(.*?)<\/li>/gi, '- $1\n');
    markdown = markdown.replace(/<\/?[uo]l[^>]*>/gi, '');

    // Horizontal rule
    markdown = markdown.replace(/<hr[^>]*>/gi, '---\n\n');

    // Paragraphs
    markdown = markdown.replace(/<p[^>]*>(.*?)<\/p>/gi, '$1\n\n');

    // Remove remaining HTML
    markdown = markdown.replace(/<[^>]+>/g, '');

    // Clean up
    markdown = markdown.replace(/\n{3,}/g, '\n\n');
    markdown = markdown.trim();

    return markdown;
}

console.log('📝 tipTapIntegration.js loaded');