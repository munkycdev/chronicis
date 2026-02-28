// ================================================
// TipTap Integration
// ================================================

// Global storage for editor instances
window.tipTapEditors = window.tipTapEditors || {};

// Wait for TipTap to be ready
window.addEventListener('tiptap-ready', function() {
    // TipTap is now loaded and ready
});

// ================================================
// HTML/MARKDOWN DETECTION & CONVERSION
// ================================================

// Detect if content is HTML or markdown
// HTML from TipTap will have tags like <p>, <h1>, <ul>, etc.
// Markdown will have #, *, -, etc. without HTML tags
function isHtmlContent(content) {
    if (!content || content.trim() === '') return false;
    
    // Check for common HTML tags that TipTap produces
    const htmlTagPattern = /<(p|h[1-6]|ul|ol|li|strong|em|a|pre|code|blockquote|div|span|br|table|thead|tbody|tr|th|td)[^>]*>/i;
    return htmlTagPattern.test(content);
}

// Ensure content is HTML - convert from markdown if needed
function ensureHtml(content) {
    if (!content || content.trim() === '') return '<p></p>';
    
    if (isHtmlContent(content)) {
        return content;
    }
    
    // Content appears to be markdown - convert to HTML
    return markdownToHTML(content);
}

// Expose for Blazor interop
window.ensureHtml = ensureHtml;
window.isHtmlContent = isHtmlContent;

// ================================================
// MARKDOWN TABLE NORMALIZATION
// ================================================

function parseMarkdownPipeRow(text) {
    const trimmed = (text || '').trim();
    if (!trimmed.startsWith('|') || !trimmed.endsWith('|')) {
        return [];
    }

    const inner = trimmed.slice(1, -1);
    return inner.split('|').map(cell => cell.trim());
}

function isMarkdownPipeRow(text) {
    const cells = parseMarkdownPipeRow(text);
    return cells.length > 0;
}

function isMarkdownSeparatorCell(cell) {
    return /^:?-{3,}:?$/.test((cell || '').trim());
}

function createTableElementFromMarkdownRows(headerCells, separatorCells, bodyRows) {
    const table = document.createElement('table');
    const thead = document.createElement('thead');
    const headerRow = document.createElement('tr');

    const columnCount = Math.max(
        headerCells.length,
        separatorCells.length,
        bodyRows.reduce((max, row) => Math.max(max, row.length), 0),
        1);

    const normalizeCells = (cells) => {
        const normalized = cells.slice(0, columnCount);
        while (normalized.length < columnCount) {
            normalized.push('');
        }
        return normalized;
    };

    normalizeCells(headerCells).forEach(cell => {
        const th = document.createElement('th');
        th.textContent = cell;
        headerRow.appendChild(th);
    });
    thead.appendChild(headerRow);
    table.appendChild(thead);

    const tbody = document.createElement('tbody');
    bodyRows.forEach(rowCells => {
        const tr = document.createElement('tr');
        normalizeCells(rowCells).forEach(cell => {
            const td = document.createElement('td');
            td.textContent = cell;
            tr.appendChild(td);
        });
        tbody.appendChild(tr);
    });
    table.appendChild(tbody);

    return table;
}

function extractLinesFromParagraph(paragraph) {
    const parts = paragraph.innerHTML.split(/<br\s*\/?>/i);
    return parts
        .map(part => {
            const temp = document.createElement('div');
            temp.innerHTML = part;
            return (temp.textContent || '').trim();
        })
        .filter(line => line.length > 0);
}

function convertMarkdownPipeTablesInHtml(html) {
    if (!html || html.indexOf('|') === -1) {
        return html;
    }

    const container = document.createElement('div');
    container.innerHTML = html;
    let changed = false;

    let i = 0;
    while (i < container.children.length) {
        const current = container.children[i];
        if (!current || current.tagName !== 'P') {
            i += 1;
            continue;
        }

        // Scenario A: single paragraph with <br>-separated markdown table lines.
        if (current.innerHTML.toLowerCase().includes('<br')) {
            const lines = extractLinesFromParagraph(current);
            if (lines.length >= 2 && isMarkdownPipeRow(lines[0]) && isMarkdownPipeRow(lines[1])) {
                const headerCells = parseMarkdownPipeRow(lines[0]);
                const separatorCells = parseMarkdownPipeRow(lines[1]);
                const isSeparator = separatorCells.length > 0 && separatorCells.every(isMarkdownSeparatorCell);

                if (isSeparator) {
                    const bodyRows = lines
                        .slice(2)
                        .filter(isMarkdownPipeRow)
                        .map(parseMarkdownPipeRow);

                    const table = createTableElementFromMarkdownRows(headerCells, separatorCells, bodyRows);
                    current.before(table);
                    current.remove();
                    changed = true;
                    i += 1;
                    continue;
                }
            }
        }

        // Scenario B: consecutive paragraphs where each paragraph is a markdown table row.
        const rowNodes = [];
        let j = i;
        while (j < container.children.length) {
            const candidate = container.children[j];
            if (!candidate || candidate.tagName !== 'P') {
                break;
            }

            const rowText = (candidate.textContent || '').trim();
            if (!isMarkdownPipeRow(rowText)) {
                break;
            }

            rowNodes.push(candidate);
            j += 1;
        }

        if (rowNodes.length >= 2) {
            const headerCells = parseMarkdownPipeRow(rowNodes[0].textContent || '');
            const separatorCells = parseMarkdownPipeRow(rowNodes[1].textContent || '');
            const isSeparator = separatorCells.length > 0 && separatorCells.every(isMarkdownSeparatorCell);

            if (isSeparator) {
                const bodyRows = rowNodes
                    .slice(2)
                    .map(node => parseMarkdownPipeRow(node.textContent || ''))
                    .filter(row => row.length > 0);

                const table = createTableElementFromMarkdownRows(headerCells, separatorCells, bodyRows);
                rowNodes[0].before(table);
                rowNodes.forEach(node => node.remove());
                changed = true;
                i += 1;
                continue;
            }
        }

        i += 1;
    }

    return changed ? container.innerHTML : html;
}

window.tipTapTableNormalizeLocks = window.tipTapTableNormalizeLocks || {};
window.tipTapTableControlTeardowns = window.tipTapTableControlTeardowns || {};

function normalizeMarkdownTablesInEditor(editor, editorId) {
    if (!editor) {
        return '';
    }

    const lockKey = editorId || editor?.options?.element?.id || '__default__';
    if (window.tipTapTableNormalizeLocks[lockKey]) {
        return editor.getHTML();
    }

    const html = editor.getHTML();
    const normalizedHtml = convertMarkdownPipeTablesInHtml(html);
    if (normalizedHtml === html) {
        return html;
    }

    window.tipTapTableNormalizeLocks[lockKey] = true;
    try {
        editor.commands.setContent(normalizedHtml, false);
    } finally {
        window.tipTapTableNormalizeLocks[lockKey] = false;
    }

    return normalizedHtml;
}

window.normalizeMarkdownTablesInEditor = normalizeMarkdownTablesInEditor;

// ================================================
// TABLE CONTROLS
// ================================================

function createTableControlButton(config, runCommand) {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'chronicis-table-control-button';
    button.setAttribute('title', config.title);
    button.setAttribute('aria-label', config.title);
    button.innerHTML = `<i class="${config.icon}" aria-hidden="true"></i><span>${config.label}</span>`;
    button.dataset.requiresTable = config.requiresTable ? 'true' : 'false';
    button.addEventListener('click', () => runCommand(config.command, config.args));
    return button;
}

function updateTipTapTableControlState(editor, controlsElement) {
    const inTable = editor?.isActive?.('table') === true;
    controlsElement.querySelectorAll('[data-requires-table="true"]').forEach(button => {
        button.disabled = !inTable;
    });
}

function disposeTipTapTableControls(editorId) {
    const teardown = window.tipTapTableControlTeardowns[editorId];
    if (typeof teardown === 'function') {
        teardown();
    }

    delete window.tipTapTableControlTeardowns[editorId];
}

function initializeTipTapTableControls(editorId, editor) {
    if (!editorId || !editor) {
        return;
    }

    disposeTipTapTableControls(editorId);

    const container = document.getElementById(editorId);
    if (!container || !container.parentElement) {
        return;
    }

    const chainProbe = editor.chain?.();
    if (!chainProbe || typeof chainProbe.insertTable !== 'function') {
        return;
    }

    const controls = document.createElement('div');
    controls.id = `chronicis-table-controls-${editorId}`;
    controls.className = 'chronicis-editor-table-controls';
    controls.setAttribute('role', 'toolbar');
    controls.setAttribute('aria-label', 'Table controls');

    const runCommand = (command, args) => {
        if (!editor || editor.isDestroyed) {
            return;
        }

        try {
            const chain = editor.chain().focus();
            if (typeof chain[command] !== 'function') {
                return;
            }

            const result = args === undefined ? chain[command]() : chain[command](args);
            if (result && typeof result.run === 'function') {
                result.run();
            }
        } catch (err) {
            console.error(`Failed to run TipTap table command '${command}'`, err);
        }

        updateTipTapTableControlState(editor, controls);
    };

    const buttonConfigs = [
        {
            label: 'Insert',
            title: 'Insert table (3x3)',
            icon: 'fa-solid fa-table',
            command: 'insertTable',
            args: { rows: 3, cols: 3, withHeaderRow: true },
            requiresTable: false
        },
        {
            label: 'Row +',
            title: 'Add row below',
            icon: 'fa-solid fa-plus',
            command: 'addRowAfter',
            requiresTable: true
        },
        {
            label: 'Col +',
            title: 'Add column to the right',
            icon: 'fa-solid fa-plus',
            command: 'addColumnAfter',
            requiresTable: true
        },
        {
            label: 'Del Row',
            title: 'Delete current row',
            icon: 'fa-solid fa-minus',
            command: 'deleteRow',
            requiresTable: true
        },
        {
            label: 'Del Col',
            title: 'Delete current column',
            icon: 'fa-solid fa-minus',
            command: 'deleteColumn',
            requiresTable: true
        },
        {
            label: 'Header',
            title: 'Toggle header row',
            icon: 'fa-solid fa-heading',
            command: 'toggleHeaderRow',
            requiresTable: true
        },
        {
            label: 'Del Table',
            title: 'Delete table',
            icon: 'fa-solid fa-trash',
            command: 'deleteTable',
            requiresTable: true
        }
    ];

    buttonConfigs
        .map(config => createTableControlButton(config, runCommand))
        .forEach(button => controls.appendChild(button));

    container.parentElement.insertBefore(controls, container);

    const updateState = () => updateTipTapTableControlState(editor, controls);
    editor.on('selectionUpdate', updateState);
    editor.on('update', updateState);

    updateState();

    window.tipTapTableControlTeardowns[editorId] = () => {
        try {
            editor.off('selectionUpdate', updateState);
            editor.off('update', updateState);
        } catch {
            // Ignore cleanup failures during disposal.
        }

        controls.remove();
    };
}

window.initializeTipTapTableControls = initializeTipTapTableControls;
window.disposeTipTapTableControls = disposeTipTapTableControls;

// ================================================
// EDITOR INITIALIZATION
// ================================================

async function initializeTipTapEditor(editorId, initialContent, dotNetHelper) {
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

    // Add wiki link extension if available
    if (window.createWikiLinkExtension) {
        try {
            const wikiLinkExt = window.createWikiLinkExtension();
            if (wikiLinkExt) {
                extensions.push(wikiLinkExt);
            } else {
                console.error('Wiki link extension returned null - TipTap.Node may not be available');
            }
        } catch (err) {
            console.error('Failed to load wiki link extension:', err);
        }
    }

    // Add external link extension if available
    if (window.createExternalLinkExtension) {
        try {
            const externalLinkExt = window.createExternalLinkExtension();
            if (externalLinkExt) {
                extensions.push(externalLinkExt);
            } else {
                console.error('External link extension returned null - TipTap.Node may not be available');
            }
        } catch (err) {
            console.error('Failed to load external link extension:', err);
        }
    }

    // Add Image extension for inline image support
    if (window.TipTap.Image) {
        extensions.push(window.TipTap.Image.configure({
            inline: false,
            allowBase64: false,
            HTMLAttributes: {
                class: 'chronicis-inline-image',
            },
        }));
    }

    // Add table extensions for rich table editing support.
    if (window.TipTap.Table && window.TipTap.TableRow && window.TipTap.TableHeader && window.TipTap.TableCell) {
        extensions.push(window.TipTap.Table.configure({ resizable: true }));
        extensions.push(window.TipTap.TableRow);
        extensions.push(window.TipTap.TableHeader);
        extensions.push(window.TipTap.TableCell);
    }

    // Create editor
    // Auto-detect if content is HTML or markdown and convert if needed
    // This provides backwards compatibility for existing markdown content
    const htmlContent = ensureHtml(initialContent);
    const normalizedInitialContent = convertMarkdownPipeTablesInHtml(htmlContent);
    const editor = new window.TipTap.Editor({
        element: container,
        extensions: extensions,
        content: normalizedInitialContent,
        editable: true,
        onUpdate: ({ editor }) => {
            const html = normalizeMarkdownTablesInEditor(editor, editorId);
            dotNetHelper.invokeMethodAsync('OnEditorUpdate', html);
        },
    });

    // Store editor instance
    window.tipTapEditors[editorId] = editor;
    initializeTipTapTableControls(editorId, editor);

    // Add click handler for wiki links
    container.addEventListener('click', (e) => {
        const wikiLink = e.target.closest('span[data-type="wiki-link"]');
        if (wikiLink) {
            e.preventDefault();
            e.stopPropagation();
            const targetArticleId = wikiLink.getAttribute('data-target-id');
            const isBroken = wikiLink.getAttribute('data-broken') === 'true';
            
            if (targetArticleId) {
                if (isBroken) {
                    // Dispatch event for broken link - Blazor will handle via event listener
                    dotNetHelper.invokeMethodAsync('OnBrokenLinkClicked', targetArticleId);
                } else {
                    // Navigate to the article
                    dotNetHelper.invokeMethodAsync('OnWikiLinkClicked', targetArticleId);
                }
            }
        }
    });

    // Add click handler for external links
    container.addEventListener('click', (e) => {
        const externalLink = e.target.closest('span[data-type="external-link"]');
        if (externalLink) {
            e.preventDefault();
            e.stopPropagation();

            const source = externalLink.getAttribute('data-source');
            const id = externalLink.getAttribute('data-id');
            const title = externalLink.getAttribute('data-title');

            if (source && id) {
                dotNetHelper.invokeMethodAsync('OnExternalLinkClicked', source, id, title || '');
            }
        }
    });

    // Add hover handler for wiki link tooltips
    let tooltipShowTimeout = null;

    container.addEventListener('mouseover', (e) => {
        const wikiLink = e.target.closest('span[data-type="wiki-link"]');
        if (wikiLink && !wikiLink.hasAttribute('data-tooltip-loading')) {
            const targetArticleId = wikiLink.getAttribute('data-target-id');
            
            // Cancel any pending hide
            cancelTooltipHide();
            
            // Clear any existing show timeout
            if (tooltipShowTimeout) {
                clearTimeout(tooltipShowTimeout);
            }
            
            // Add slight delay to avoid flickering on quick mouse passes
            tooltipShowTimeout = setTimeout(async () => {
                if (!targetArticleId) return;
                
                // Mark as loading to prevent duplicate requests
                wikiLink.setAttribute('data-tooltip-loading', 'true');
                
                try {
                    // First try to get AI summary preview
                    const summaryPreview = await dotNetHelper.invokeMethodAsync('GetArticleSummaryPreview', targetArticleId);
                    
                    if (summaryPreview && summaryPreview.summary) {
                        // Show rich summary tooltip
                        showSummaryTooltip(wikiLink, summaryPreview);
                    } else {
                        // Fall back to path tooltip
                        const path = await dotNetHelper.invokeMethodAsync('GetArticlePath', targetArticleId);
                        if (path) {
                            showWikiLinkTooltip(wikiLink, path);
                        }
                    }
                } catch (err) {
                    console.error('Error getting tooltip data:', err);
                } finally {
                    wikiLink.removeAttribute('data-tooltip-loading');
                }
            }, 300); // 300ms delay
        }
    });

    container.addEventListener('mouseout', (e) => {
        const wikiLink = e.target.closest('span[data-type="wiki-link"]');
        if (wikiLink) {
            if (tooltipShowTimeout) {
                clearTimeout(tooltipShowTimeout);
                tooltipShowTimeout = null;
            }
            // Delay hide to allow mouse to reach tooltip
            scheduleTooltipHide(100);
        }
    });

    return editor;
}

function destroyTipTapEditor(editorId) {
    const editor = window.tipTapEditors[editorId];
    if (editor) {
        disposeTipTapTableControls(editorId);
        editor.destroy();
        delete window.tipTapEditors[editorId];
    }
}

function setTipTapContent(editorId, content) {
    const editor = window.tipTapEditors[editorId];
    if (editor) {
        // Auto-detect if content is HTML or markdown and convert if needed
        const htmlContent = ensureHtml(content);
        const normalizedHtml = convertMarkdownPipeTablesInHtml(htmlContent);
        editor.commands.setContent(normalizedHtml);
    } else {
        console.error(`Editor not found: ${editorId}`);
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ================================================
// WIKI LINK TOOLTIP
// ================================================

let currentWikiLinkTooltip = null;
let globalTooltipHideTimeout = null;

function cancelTooltipHide() {
    if (globalTooltipHideTimeout) {
        clearTimeout(globalTooltipHideTimeout);
        globalTooltipHideTimeout = null;
    }
}

function scheduleTooltipHide(delay = 100) {
    cancelTooltipHide();
    globalTooltipHideTimeout = setTimeout(() => {
        hideWikiLinkTooltip();
    }, delay);
}

function showWikiLinkTooltip(element, path) {
    hideWikiLinkTooltip();
    
    const tooltip = document.createElement('div');
    tooltip.className = 'wiki-link-tooltip';
    tooltip.textContent = path;
    
    positionTooltip(tooltip, element);
    
    document.body.appendChild(tooltip);
    currentWikiLinkTooltip = tooltip;
    
    addTooltipHoverHandlers(tooltip);
}

function showSummaryTooltip(element, preview) {
    hideWikiLinkTooltip();
    
    const tooltip = document.createElement('div');
    tooltip.className = 'wiki-link-tooltip wiki-link-tooltip--summary';
    
    // Build tooltip content
    const header = document.createElement('div');
    header.className = 'wiki-link-tooltip__header';
    
    const title = document.createElement('span');
    title.className = 'wiki-link-tooltip__title';
    title.textContent = preview.title;
    header.appendChild(title);
    
    const badge = document.createElement('span');
    badge.className = 'wiki-link-tooltip__badge';
    badge.textContent = 'AI Summary';
    header.appendChild(badge);
    
    tooltip.appendChild(header);
    
    const summary = document.createElement('div');
    summary.className = 'wiki-link-tooltip__summary';
    summary.textContent = preview.summary;
    tooltip.appendChild(summary);
    
    positionTooltip(tooltip, element);
    
    document.body.appendChild(tooltip);
    currentWikiLinkTooltip = tooltip;
    
    addTooltipHoverHandlers(tooltip);
}

function positionTooltip(tooltip, element) {
    const rect = element.getBoundingClientRect();
    tooltip.style.position = 'fixed';
    tooltip.style.left = `${rect.left}px`;
    tooltip.style.top = `${rect.top - 8}px`;
    tooltip.style.transform = 'translateY(-100%)';
    tooltip.style.zIndex = '10000';
}

function addTooltipHoverHandlers(tooltip) {
    tooltip.addEventListener('mouseenter', () => {
        // Cancel any pending hide when mouse enters tooltip
        cancelTooltipHide();
    });
    tooltip.addEventListener('mouseleave', () => {
        hideWikiLinkTooltip();
    });
}

function hideWikiLinkTooltip() {
    if (currentWikiLinkTooltip) {
        currentWikiLinkTooltip.remove();
        currentWikiLinkTooltip = null;
    }
}

// ================================================
// MARKDOWN <-> HTML CONVERSION
// ================================================

function markdownToHTML(markdown) {
    if (!markdown) return '<p></p>';

    let html = markdown;

    // External links: [[source|id|title]]
    // Convert to: <span data-type="external-link" data-source="source" data-id="id" data-title="title">...</span>
    html = html.replace(/\[\[([^|\]]+)\|([^|\]]+)\|([^\]]+)\]\]/g, (match, source, id, title) => {
        const safeSource = escapeHtml(source.trim());
        const safeId = escapeHtml(id.trim());
        const safeTitle = escapeHtml(title.trim());
        const sourceLabel = safeSource ? safeSource.toUpperCase() : 'EXT';
        return `<span data-type="external-link" class="external-link-node" data-source="${safeSource}" data-id="${safeId}" data-title="${safeTitle}"><span class="external-link-badge">${sourceLabel}</span><span class="external-link-text">${safeTitle}</span><i class="external-link-icon fa-solid fa-arrow-up-right-from-square" aria-hidden="true"></i></span>`;
    });

    // Wiki links: [[guid]] or [[guid|display text]]
    // Convert to: <span data-type="wiki-link" data-target-id="guid" data-display="display">display</span>
    html = html.replace(/\[\[([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(?:\|([^\]]+))?\]\]/g, (match, guid, display) => {
        const displayText = display || 'Loading...';
        const displayAttr = display ? ` data-display="${display}"` : '';
        return `<span data-type="wiki-link" data-target-id="${guid}"${displayAttr}>${displayText}</span>`;
    });

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
    html = html.replace(/\*([^\*\n]+?)\*/g, '<em>$1</em>');

    // Links [text](url)
    html = html.replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2">$1</a>');

    // Code blocks ```
    html = html.replace(/```([\s\S]*?)```/g, '<pre><code>$1</code></pre>');

    // Inline code `
    html = html.replace(/`([^`]+)`/g, '<code>$1</code>');

    // Bullet lists - find consecutive lines starting with * or -
    html = html.replace(/^([\*\-]\s+.+\n?)+/gm, (match) => {
        const items = match.trim().split('\n')
            .filter(line => line.trim())
            .map(line => {
                const content = line.replace(/^[\*\-]\s+/, '');
                return `<li>${content}</li>`;
            })
            .join('');
        return `<ul class="chronicis-bullet-list">${items}</ul>`;
    });

    // Ordered lists - find consecutive lines starting with numbers
    html = html.replace(/^(\d+\.\s+.+\n?)+/gm, (match) => {
        const items = match.trim().split('\n')
            .filter(line => line.trim())
            .map(line => {
                const content = line.replace(/^\d+\.\s+/, '');
                return `<li>${content}</li>`;
            })
            .join('');
        return `<ol class="chronicis-ordered-list">${items}</ol>`;
    });

    // Line breaks (but not inside lists which are already processed)
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

    // External links: <span data-type="external-link" data-source="source" data-id="id" data-title="title">...</span>
    // Convert to: [[source|id|title]]
    markdown = markdown.replace(/<span[^>]*data-type="external-link"[^>]*data-source="([^"]+)"[^>]*data-id="([^"]+)"[^>]*data-title="([^"]*)"[^>]*>.*?<\/span>/gi, '[[$1|$2|$3]]');

    // Wiki links: <span data-type="wiki-link" data-target-id="guid" data-display="display">text</span>
    // Convert to: [[guid|display]] or [[guid]]
    markdown = markdown.replace(/<span[^>]*data-type="wiki-link"[^>]*data-target-id="([^"]+)"[^>]*data-display="([^"]+)"[^>]*>.*?<\/span>/gi, '[[$1|$2]]');
    markdown = markdown.replace(/<span[^>]*data-type="wiki-link"[^>]*data-target-id="([^"]+)"[^>]*>.*?<\/span>/gi, '[[$1]]');

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

    // Lists - handle the entire ul/ol block at once
    markdown = markdown.replace(/<ul[^>]*>([\s\S]*?)<\/ul>/gi, (match, inner) => {
        // Extract all li contents, handling potential <p> tags inside
        const items = [];
        const liRegex = /<li[^>]*>([\s\S]*?)<\/li>/gi;
        let liMatch;
        while ((liMatch = liRegex.exec(inner)) !== null) {
            // Strip any <p> tags from inside the li
            let content = liMatch[1].replace(/<p[^>]*>(.*?)<\/p>/gi, '$1').trim();
            items.push('* ' + content);
        }
        return items.join('\n') + '\n';
    });

    markdown = markdown.replace(/<ol[^>]*>([\s\S]*?)<\/ol>/gi, (match, inner) => {
        const items = [];
        const liRegex = /<li[^>]*>([\s\S]*?)<\/li>/gi;
        let liMatch;
        let counter = 1;
        while ((liMatch = liRegex.exec(inner)) !== null) {
            let content = liMatch[1].replace(/<p[^>]*>(.*?)<\/p>/gi, '$1').trim();
            items.push(counter + '. ' + content);
            counter++;
        }
        return items.join('\n') + '\n';
    });

    // Remove remaining HTML tags
    markdown = markdown.replace(/<p[^>]*>/gi, '');
    markdown = markdown.replace(/<\/p>/gi, '\n\n');
    markdown = markdown.replace(/<br\s*\/?>/gi, '\n');
    
    // Iteratively remove HTML tags to handle malformed/nested tags
    // This prevents bypass via constructs like <script<script>>
    let previousLength;
    do {
        previousLength = markdown.length;
        markdown = markdown.replace(/<[^>]+>/g, '');
    } while (markdown.length !== previousLength && markdown.includes('<'));

    // Clean up extra whitespace
    markdown = markdown.replace(/\n{3,}/g, '\n\n');
    markdown = markdown.trim();

    return markdown;
}
