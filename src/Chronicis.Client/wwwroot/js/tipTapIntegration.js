// ================================================
// TipTap Integration - Phase 7.3 Complete
// ================================================

// Global storage for editor instances and tooltips
window.tipTapEditors = window.tipTapEditors || {};
window.activeTooltip = null;
window.tooltipHideTimeout = null;

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

    // Load hashtag extension
    try {
        const hashtagModule = await import('/js/tipTapHashtagExtension.js');
        const HashtagExtension = await hashtagModule.createHashtagExtension();
        extensions.push(HashtagExtension);
        console.log('✅ Hashtag extension loaded and added');
    } catch (error) {
        console.warn('⚠️ Could not load hashtag extension:', error.message);
    }

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

    // Setup hashtag interactions (Phase 7.3)
    setupHashtagClickHandler(editorId, editor);
    setupHashtagHoverHandler(editorId, editor);

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

// ================================================
// PHASE 7.3: HASHTAG CLICK HANDLER
// ================================================

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

            console.log(`Hashtag clicked: #${hashtagName}, linked: ${isLinked}`);

            if (isLinked && articleSlug) {
                // Navigate to linked article
                console.log(`Navigating to: /article/${articleSlug}`);
                window.location.href = `/article/${articleSlug}`;
            } else if (hashtagName) {
                // Trigger linking dialog (Phase 7.3)
                console.log(`Triggering link dialog for #${hashtagName}`);
                
                // Dispatch event that Blazor can listen to
                const event = new CustomEvent('hashtag-link-requested', {
                    detail: { hashtagName: hashtagName }
                });
                document.dispatchEvent(event);
            }

            e.preventDefault();
            e.stopPropagation();
        }
    });

    console.log(`✅ Click handler setup for ${editorId}`);
}

// ================================================
// PHASE 7.3: HASHTAG HOVER HANDLER
// ================================================

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

            // Hide tooltip after a delay (allows moving to tooltip)
            window.tooltipHideTimeout = setTimeout(() => {
                hideHashtagTooltip();
            }, 200);
        }
    });

    console.log(`✅ Hover handler setup for ${editorId}`);
}

async function showHashtagTooltip(element, hashtagName) {
    // Remove existing tooltip
    hideHashtagTooltip();

    try {
        // Fetch hashtag preview from API
        const response = await fetch(`/api/hashtags/${encodeURIComponent(hashtagName)}/preview`);
        if (!response.ok) {
            console.error('Failed to fetch hashtag preview');
            return;
        }

        const preview = await response.json();

        if (!preview.hasArticle) {
            // Show "not linked" tooltip
            createTooltip(element, `
                <div class="hashtag-tooltip-content">
                    <div class="hashtag-tooltip-title">#${hashtagName}</div>
                    <div class="hashtag-tooltip-text">Not linked to an article</div>
                    <div class="hashtag-tooltip-action">Click to link</div>
                </div>
            `);
            return;
        }

        // Show article preview tooltip
        createTooltip(element, `
            <div class="hashtag-tooltip-content">
                <div class="hashtag-tooltip-title">${escapeHtml(preview.articleTitle || '(Untitled)')}</div>
                <div class="hashtag-tooltip-text">${escapeHtml(preview.previewText || 'No content')}</div>
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
    tooltip.style.zIndex = '10000';

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

    // Convert hashtags FIRST (before headers to avoid confusion)
    // This will be enhanced in Phase 7.3 to check if hashtags are linked
    html = html.replace(
        /#(\w+)/g,
        '<span data-type="hashtag" class="chronicis-hashtag" data-hashtag-name="$1" data-linked="false" title="Hashtag">#$1</span>'
    );

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

    // Convert hashtag spans back to plain text
    markdown = markdown.replace(
        /<span[^>]*data-type="hashtag"[^>]*data-hashtag-name="([^"]*)"[^>]*>.*?<\/span>/gi,
        '#$1'
    );

    // Fallback patterns
    markdown = markdown.replace(/<span[^>]*data-type="hashtag"[^>]*>(#\w+)<\/span>/gi, '$1');
    markdown = markdown.replace(/<span[^>]*class="chronicis-hashtag"[^>]*>(#\w+)<\/span>/gi, '$1');

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

console.log('✅ TipTap integration script loaded (Phase 7.3)');
