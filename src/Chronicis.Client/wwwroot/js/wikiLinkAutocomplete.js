// ============================================
// WIKI LINK AUTOCOMPLETE INTEGRATION
// ============================================
// Detects [[ typing and triggers autocomplete
// ============================================

/**
 * Initialize wiki link autocomplete for an editor
 * @param {string} editorId - The editor container ID
 * @param {object} dotNetHelper - DotNetObjectReference for callbacks
 */
function initializeWikiLinkAutocomplete(editorId, dotNetHelper) {
    const container = document.getElementById(editorId);
    if (!container) {
        console.error('Container not found for wiki link autocomplete:', editorId);
        return;
    }

    // JS-side flag that is the authoritative source of truth for whether the autocomplete
    // popup is open. This avoids relying on DOM queries which are subject to Blazor render
    // timing gaps — the element may still be in the DOM while Blazor is processing the
    // re-render that removes it, causing arrow keys to be incorrectly swallowed.
    let autocompleteVisible = false;

    // Listen for input events to detect [[ typing
    container.addEventListener('input', (e) => {
        const editor = window.tipTapEditors[editorId];
        if (!editor) return;

        // Get cursor position
        const { from } = editor.state.selection;
        const textBefore = editor.state.doc.textBetween(Math.max(0, from - 50), from, '\n');

        // Check if we just typed [[
        const match = textBefore.match(/\[\[([^\]]*)$/);
        
        if (match) {
            const fullQuery = match[1];
            
            // Check for pipe character - if present, only search on the part before it
            const pipeIndex = fullQuery.indexOf('|');
            const searchQuery = pipeIndex >= 0 ? fullQuery.substring(0, pipeIndex) : fullQuery;
            const customDisplayText = pipeIndex >= 0 ? fullQuery.substring(pipeIndex + 1) : null;
            
            // Store custom display text for later use when inserting
            window._wikiLinkCustomDisplayText = customDisplayText;
            
            // Get cursor position for autocomplete placement
            const coords = editor.view.coordsAtPos(from);
            
            // IMPORTANT: coordsAtPos returns coordinates relative to the VIEWPORT, not the document
            // This means they automatically account for scroll position
            let x = coords.left;
            let y = coords.bottom;
            
            // Add some padding below the cursor
            y += 4;
            
            // Get viewport dimensions
            const viewportWidth = window.innerWidth;
            const viewportHeight = window.innerHeight;
            
            // Estimated autocomplete dimensions (will be adjusted by CSS if needed)
            const autocompleteWidth = 300;
            const autocompleteHeight = 300; // max-height from CSS
            
            // Adjust X if popup would overflow right edge
            if (x + autocompleteWidth > viewportWidth) {
                x = viewportWidth - autocompleteWidth - 16; // 16px padding from edge
            }
            
            // Adjust Y if popup would overflow bottom edge - position above cursor instead
            if (y + autocompleteHeight > viewportHeight) {
                y = coords.top - autocompleteHeight - 4; // Position above cursor with padding
                
                // If that would overflow the top, just position at top with padding
                if (y < 0) {
                    y = 16;
                }
            }
            
            // Ensure minimum padding from left edge
            if (x < 16) {
                x = 16;
            }
            
            // Mark autocomplete as visible synchronously before the async Blazor call so that
            // the keydown handler sees the correct state on the very next keystroke.
            autocompleteVisible = true;

            // Notify Blazor to show autocomplete (only search on the part before pipe)
            dotNetHelper.invokeMethodAsync('OnAutocompleteTriggered', searchQuery, x, y);
        } else {
            // No [[ found — hide autocomplete immediately (synchronously).
            autocompleteVisible = false;
            window._wikiLinkCustomDisplayText = null;
            dotNetHelper.invokeMethodAsync('OnAutocompleteHidden');
        }
    });

    // Listen for keyboard events on the container.
    // IMPORTANT: Only intercept arrow/enter/escape when the autocomplete is actually open.
    // Use the local JS flag rather than a DOM query to avoid Blazor render timing races
    // where the element may still be present in the DOM while being removed by re-render,
    // which would incorrectly swallow ArrowUp/ArrowDown and break cursor movement.
    container.addEventListener('keydown', (e) => {
        const editor = window.tipTapEditors[editorId];
        if (!editor) return;

        // Fast-exit for any key we never need to intercept — preserves all normal editing.
        if (e.key !== 'ArrowDown' && e.key !== 'ArrowUp' && e.key !== 'Enter' && e.key !== 'Escape') return;

        // Only steal these keys when the autocomplete popup is open.
        if (!autocompleteVisible) return;

        e.preventDefault();
        e.stopPropagation();

        if (e.key === 'ArrowDown') {
            dotNetHelper.invokeMethodAsync('OnAutocompleteArrowDown');
        } else if (e.key === 'ArrowUp') {
            dotNetHelper.invokeMethodAsync('OnAutocompleteArrowUp');
        } else if (e.key === 'Enter') {
            dotNetHelper.invokeMethodAsync('OnAutocompleteEnter');
        } else if (e.key === 'Escape') {
            autocompleteVisible = false;
            dotNetHelper.invokeMethodAsync('OnAutocompleteHidden');
        }
    });

    // Listen for scroll events to hide autocomplete.
    // The editor content is scrollable, so we need to hide autocomplete on scroll.
    const editorElement = container.querySelector('.ProseMirror');
    if (editorElement) {
        editorElement.addEventListener('scroll', () => {
            if (autocompleteVisible) {
                autocompleteVisible = false;
                dotNetHelper.invokeMethodAsync('OnAutocompleteHidden');
            }
        }, { passive: true });
    }

    // Also listen for window scroll (in case the whole page scrolls)
    window.addEventListener('scroll', () => {
        if (autocompleteVisible) {
            autocompleteVisible = false;
            dotNetHelper.invokeMethodAsync('OnAutocompleteHidden');
        }
    }, { passive: true });

    // Expose a setter so insertWikiLink / insertExternalLinkToken can close the popup
    // without needing access to the closure-scoped flag directly.
    window._setAutocompleteVisible = (value) => { autocompleteVisible = value; };
}

/**
 * Insert a wiki link at the current cursor position, replacing the [[ trigger text.
 * @param {string} editorId - The editor container ID
 * @param {string} articleId - The GUID of the article to link
 * @param {string} displayText - The text to display
 */
function insertWikiLink(editorId, articleId, displayText) {
    const editor = window.tipTapEditors[editorId];
    if (!editor) {
        console.error('Editor not found:', editorId);
        return;
    }

    const customDisplayText = window._wikiLinkCustomDisplayText;
    const finalDisplayText = customDisplayText && customDisplayText.trim() ? customDisplayText.trim() : displayText;

    window._wikiLinkCustomDisplayText = null;
    if (window._setAutocompleteVisible) window._setAutocompleteVisible(false);

    const { from } = editor.state.selection;
    const doc = editor.state.doc;
    let bracketPos = -1;

    for (let pos = from - 1; pos >= Math.max(0, from - 100); pos--) {
        try {
            const char1 = doc.textBetween(pos, pos + 1, '');
            const char2 = pos > 0 ? doc.textBetween(pos - 1, pos, '') : '';
            if (char2 === '[' && char1 === '[') {
                bracketPos = pos - 1;
                break;
            }
        } catch (e) {
            continue;
        }
    }

    if (bracketPos === -1) {
        console.error('Could not find [[ before cursor');
        return;
    }

    editor
        .chain()
        .focus()
        .deleteRange({ from: bracketPos, to: from })
        .insertContent({ type: 'wikiLink', attrs: { targetArticleId: articleId, displayText: finalDisplayText, broken: false } })
        .insertContent(' ')
        .run();
}

/**
 * Insert an external link token at the current cursor position.
 * @param {string} editorId - The editor container ID
 * @param {string} source - External source key
 * @param {string} id - External id value
 * @param {string} title - Display title
 */
function insertExternalLinkToken(editorId, source, id, title) {
    const editor = window.tipTapEditors[editorId];
    if (!editor) {
        console.error('Editor not found:', editorId);
        return;
    }

    if (!source || !id) {
        console.error('External link token missing source or id');
        return;
    }

    const customDisplayText = window._wikiLinkCustomDisplayText;
    const finalTitle = customDisplayText && customDisplayText.trim() ? customDisplayText.trim() : title;

    window._wikiLinkCustomDisplayText = null;
    if (window._setAutocompleteVisible) window._setAutocompleteVisible(false);

    const { from } = editor.state.selection;
    const doc = editor.state.doc;
    let bracketPos = -1;

    for (let pos = from - 1; pos >= Math.max(0, from - 100); pos--) {
        try {
            const char1 = doc.textBetween(pos, pos + 1, '');
            const char2 = pos > 0 ? doc.textBetween(pos - 1, pos, '') : '';
            if (char2 === '[' && char1 === '[') {
                bracketPos = pos - 1;
                break;
            }
        } catch (e) {
            continue;
        }
    }

    if (bracketPos === -1) {
        console.error('Could not find [[ before cursor');
        return;
    }

    editor
        .chain()
        .focus()
        .deleteRange({ from: bracketPos, to: from })
        .insertContent({ type: 'externalLink', attrs: { source: source, externalId: id, title: finalTitle } })
        .insertContent(' ')
        .run();
}

/**
 * Update the autocomplete text (for category selection).
 * @param {string} editorId - The editor container ID
 * @param {string} newText - The new text to insert (e.g., "srd/spells/")
 */
function updateAutocompleteText(editorId, newText) {
    const editor = window.tipTapEditors[editorId];
    if (!editor) {
        console.error('Editor not found:', editorId);
        return;
    }

    const { from } = editor.state.selection;
    const doc = editor.state.doc;
    let bracketPos = -1;

    for (let pos = from - 1; pos >= Math.max(0, from - 100); pos--) {
        try {
            const char1 = doc.textBetween(pos, pos + 1, '');
            const char2 = pos > 0 ? doc.textBetween(pos - 1, pos, '') : '';
            if (char2 === '[' && char1 === '[') {
                bracketPos = pos - 1;
                break;
            }
        } catch (e) {
            continue;
        }
    }

    if (bracketPos === -1) {
        console.error('Could not find [[ before cursor');
        return;
    }

    editor.chain().focus().deleteRange({ from: bracketPos, to: from }).insertContent('[[' + newText).run();
}

/**
 * Insert multiple wiki links at specified HTML character positions.
 * @param {string} editorId - The editor container ID
 * @param {Array<{articleId: string, displayText: string, startIndex: number, endIndex: number}>} matches
 */
function insertWikiLinksAtPositions(editorId, matches) {
    const editor = window.tipTapEditors[editorId];
    if (!editor) {
        console.error('Editor not found:', editorId);
        return;
    }

    if (!matches || matches.length === 0) {
        return;
    }

    let html = editor.getHTML();

    // Sort descending so replacements from the end don't shift earlier offsets
    const sortedMatches = [...matches].sort((a, b) => b.startIndex - a.startIndex);

    for (const match of sortedMatches) {
        const { articleId, displayText, startIndex, endIndex } = match;

        if (startIndex < 0 || endIndex > html.length || startIndex >= endIndex) {
            console.warn('insertWikiLinksAtPositions: skipping out-of-range match', match);
            continue;
        }

        const textAtPosition = html.substring(startIndex, endIndex);
        if (textAtPosition.toLowerCase() !== (displayText || '').toLowerCase()) {
            console.warn(`insertWikiLinksAtPositions: text mismatch at ${startIndex}-${endIndex}: expected "${displayText}", found "${textAtPosition}"`);
            continue;
        }

        const wikiLinkHtml = `<span data-type="wiki-link" class="wiki-link-node" data-target-id="${articleId}" data-display="${escapeHtmlAttr(displayText)}">${escapeHtml(displayText)}</span>`;
        html = html.substring(0, startIndex) + wikiLinkHtml + html.substring(endIndex);
    }

    editor.commands.setContent(html);
    console.log(`Inserted ${matches.length} wiki links`);
}

/**
 * Get the current HTML content from the editor.
 * @param {string} editorId - The editor container ID
 * @returns {string} The HTML content
 */
function getTipTapContent(editorId) {
    const editor = window.tipTapEditors[editorId];
    if (!editor) {
        console.error('Editor not found:', editorId);
        return '';
    }
    return editor.getHTML();
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function escapeHtmlAttr(text) {
    if (!text) return '';
    return text
        .replace(/&/g, '&amp;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;');
}

// Make all functions globally available
window.initializeWikiLinkAutocomplete = initializeWikiLinkAutocomplete;
window.insertWikiLink = insertWikiLink;
window.insertExternalLinkToken = insertExternalLinkToken;
window.updateAutocompleteText = updateAutocompleteText;
window.insertWikiLinksAtPositions = insertWikiLinksAtPositions;
window.getTipTapContent = getTipTapContent;
