// tipTapIntegration.js - UPDATED with hashtag extension support (Phase 6)
// Place this in wwwroot/js/tipTapIntegration.js

// Store editor instances
let editorInstances = {};
let tiptapReady = false;

// Global storage for editor instances and tooltips
window.tipTapEditors = window.tipTapEditors || {};
window.activeTooltip = null;

// Listen for TipTap ready event
window.addEventListener('tiptap-ready', function () {
    tiptapReady = true;
    console.log('📝 tipTapIntegration: TipTap is ready');
});

window.initializeTipTapEditor = async (editorId, initialContent, dotNetHelper) => {
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
    await createEditor(editorId, initialContent, dotNetHelper);
};

async function createEditor(editorId, initialContent, dotNetHelper) {
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
        console.log('🔨 Creating TipTap editor with hashtag support...');
        console.log('   Container:', container);
        console.log('   Initial content length:', initialContent ? initialContent.length : 0);

        // Build extensions array
        const extensions = [
            window.TipTap.StarterKit.configure({
                heading: {
                    levels: [1, 2, 3, 4, 5, 6],
                },
            })
        ];

        // Try to load hashtag extension
        try {
            const hashtagModule = await import('/js/tipTapHashtagExtension.js');
            const HashtagExtension = await hashtagModule.createHashtagExtension();
            extensions.push(HashtagExtension);
            console.log('✅ Hashtag extension loaded and added');
        } catch (error) {
            console.warn('⚠️ Could not load hashtag extension:', error.message);
            console.warn('   Editor will work without hashtag styling');
        }

        // Initialize TipTap
        const editor = new window.TipTap.Editor({
            element: container,
            extensions: extensions,
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
                const html = editor.getHTML();
                const markdown = htmlToMarkdown(html);

                // Debug: Log conversion (remove after testing)
                if (html.includes('hashtag')) {
                    console.log('🔍 HTML contains hashtag span:', html.substring(0, 500));
                    console.log('🔍 Converted to markdown:', markdown.substring(0, 500));
                }

                // Notify Blazor
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnEditorUpdate', markdown);
                }
            },
        });

        // Store instance
        editorInstances[editorId] = editor;


        setupHashtagClickHandler(editorId, editor);
        setupHashtagHoverHandler(editorId, editor);

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
// Note: Hashtags are preserved as plain text and the TipTap extension handles them
function markdownToHTML(markdown) {
    if (!markdown) return '<p></p>';

    let html = markdown;

    // Phase 6: Convert hashtags to spans FIRST (before other markdown processing)
    // This ensures hashtags are properly styled when loading existing content
    html = html.replace(/#(\w+)/g, '<span data-type="hashtag" class="chronicis-hashtag" data-hashtag-name="$1" title="Hashtag (not yet linked)">#$1</span>');

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
// Note: Hashtag spans are converted back to plain #hashtag text
function htmlToMarkdown(html) {
    if (!html) return '';

    let markdown = html;

    // Phase 6: Convert hashtag spans back to plain text
    // The TipTap extension wraps hashtags in <span data-type="hashtag">
    // Match: <span data-type="hashtag" ... data-hashtag-name="waterdeep">#Waterdeep</span>
    // Convert to: #waterdeep (use the data attribute, not the visible text)
    markdown = markdown.replace(/<span[^>]*data-type="hashtag"[^>]*data-hashtag-name="([^"]*)"[^>]*>.*?<\/span>/gi, '#$1');

    // Fallback: If no data-hashtag-name attribute, extract from content
    markdown = markdown.replace(/<span[^>]*data-type="hashtag"[^>]*>(#\w+)<\/span>/gi, '$1');
    markdown = markdown.replace(/<span[^>]*class="chronicis-hashtag"[^>]*>(#\w+)<\/span>/gi, '$1');

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

function setupHashtagClickHandler(editorId, editor) {
    const editorElement = document.getElementById(editorId);
    if (!editorElement) return;

    editorElement.addEventListener('click', async (e) => {
        const target = e.target;

        // Check if clicked element is a hashtag
        if (target.classList.contains('chronicis-hashtag')) {
            const hashtagName = target.getAttribute('data-hashtag-name');
            const isLinked = target.getAttribute('data-linked') === 'true';
            const articleSlug = target.getAttribute('data-article-slug');

            if (isLinked && articleSlug) {
                // Navigate to linked article
                window.location.href = `/article/${articleSlug}`;
            } else if (hashtagName) {
                // Show linking UI (Phase 7.3)
                console.log(`Hashtag #${hashtagName} is not linked to an article`);
                // TODO: Show linking dialog
            }

            e.preventDefault();
            e.stopPropagation();
        }
    });
}

function setupHashtagHoverHandler(editorId, editor) {
    const editorElement = document.getElementById(editorId);
    if (!editorElement) return;

    let hoverTimeout;

    editorElement.addEventListener('mouseover', async (e) => {
        const target = e.target;

        if (target.classList.contains('chronicis-hashtag')) {
            const hashtagName = target.getAttribute('data-hashtag-name');

            // Clear existing timeout
            if (hoverTimeout) clearTimeout(hoverTimeout);

            // Wait 300ms before showing tooltip
            hoverTimeout = setTimeout(async () => {
                await showHashtagTooltip(target, hashtagName);
            }, 300);
        }
    });

    editorElement.addEventListener('mouseout', (e) => {
        const target = e.target;

        if (target.classList.contains('chronicis-hashtag')) {
            // Clear timeout
            if (hoverTimeout) clearTimeout(hoverTimeout);

            // Hide tooltip after a delay
            setTimeout(() => {
                hideHashtagTooltip();
            }, 200);
        }
    });
}

async function showHashtagTooltip(element, hashtagName) {
    // Remove existing tooltip
    hideHashtagTooltip();

    try {
        // Fetch hashtag preview from API
        const response = await fetch(`/api/hashtags/${encodeURIComponent(hashtagName)}/preview`);
        if (!response.ok) return;

        const preview = await response.json();

        if (!preview.hasArticle) {
            // Show "not linked" tooltip
            createTooltip(element, `
                <div class="hashtag-tooltip-content">
                    <div class="hashtag-tooltip-title">#${hashtagName}</div>
                    <div class="hashtag-tooltip-text">Not linked to an article</div>
                </div>
            `);
            return;
        }

        // Show article preview tooltip
        createTooltip(element, `
            <div class="hashtag-tooltip-content">
                <div class="hashtag-tooltip-title">${preview.articleTitle || '(Untitled)'}</div>
                <div class="hashtag-tooltip-text">${preview.previewText || 'No content'}</div>
                <div class="hashtag-tooltip-meta">Click to open</div>
            </div>
        `);

    } catch (error) {
        console.error('Error fetching hashtag preview:', error);
    }
}

function createTooltip(element, htmlContent) {
    const tooltip = document.createElement('div');
    tooltip.className = 'hashtag-tooltip';
    tooltip.innerHTML = htmlContent;

    // Position tooltip
    const rect = element.getBoundingClientRect();
    tooltip.style.position = 'fixed';
    tooltip.style.left = `${rect.left}px`;
    tooltip.style.top = `${rect.bottom + 8}px`;

    document.body.appendChild(tooltip);
    window.activeTooltip = tooltip;

    // Add hover handlers to keep tooltip visible
    tooltip.addEventListener('mouseenter', () => {
        clearTimeout(window.tooltipHideTimeout);
    });

    tooltip.addEventListener('mouseleave', () => {
        hideHashtagTooltip();
    });
}

function hideHashtagTooltip() {
    if (window.activeTooltip) {
        window.activeTooltip.remove();
        window.activeTooltip = null;
    }
}

console.log('📝 tipTapIntegration.js loaded with hashtag support');