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
            
            // Notify Blazor to show autocomplete (only search on the part before pipe)
            dotNetHelper.invokeMethodAsync('OnAutocompleteTriggered', searchQuery, x, y);
        } else {
            // No [[ found, hide autocomplete
            window._wikiLinkCustomDisplayText = null;
            dotNetHelper.invokeMethodAsync('OnAutocompleteHidden');
        }
    });

    // Listen for keyboard events
    container.addEventListener('keydown', (e) => {
        const editor = window.tipTapEditors[editorId];
        if (!editor) return;

        // Check if autocomplete is visible by querying the DOM
        const autocomplete = document.querySelector('.wiki-link-autocomplete');
        if (!autocomplete) return; // Autocomplete not visible

        // Arrow Down - move selection down
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            dotNetHelper.invokeMethodAsync('OnAutocompleteArrowDown');
        }
        // Arrow Up - move selection up
        else if (e.key === 'ArrowUp') {
            e.preventDefault();
            dotNetHelper.invokeMethodAsync('OnAutocompleteArrowUp');
        }
        // Enter - select current item
        else if (e.key === 'Enter') {
            e.preventDefault();
            dotNetHelper.invokeMethodAsync('OnAutocompleteEnter');
        }
        // Escape - hide autocomplete
        else if (e.key === 'Escape') {
            e.preventDefault();
            dotNetHelper.invokeMethodAsync('OnAutocompleteHidden');
        }
    });

    // Listen for scroll events to hide autocomplete
    // The editor content is scrollable, so we need to hide autocomplete on scroll
    const editorElement = container.querySelector('.ProseMirror');
    if (editorElement) {
        editorElement.addEventListener('scroll', () => {
            // Check if autocomplete is visible
            const autocomplete = document.querySelector('.wiki-link-autocomplete');
            if (autocomplete) {
                dotNetHelper.invokeMethodAsync('OnAutocompleteHidden');
            }
        }, { passive: true });
    }

    // Also listen for window scroll (in case the whole page scrolls)
    window.addEventListener('scroll', () => {
        const autocomplete = document.querySelector('.wiki-link-autocomplete');
        if (autocomplete) {
            dotNetHelper.invokeMethodAsync('OnAutocompleteHidden');
        }
    }, { passive: true });

}

/**
 * Insert a wiki link at the current cursor position
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

    // Check if user provided custom display text via pipe syntax
    const customDisplayText = window._wikiLinkCustomDisplayText;
    const finalDisplayText = customDisplayText && customDisplayText.trim() ? customDisplayText.trim() : displayText;
    
    // Clear the stored custom display text
    window._wikiLinkCustomDisplayText = null;

    const { from } = editor.state.selection;
    
    // Search backwards from cursor to find [[
    // We need to search character by character in the document to get accurate positions
    const doc = editor.state.doc;
    let bracketPos = -1;
    
    // Search backwards from cursor position
    for (let pos = from - 1; pos >= Math.max(0, from - 100); pos--) {
        try {
            // Get text at this position and the next
            const char1 = doc.textBetween(pos, pos + 1, '');
            const char2 = pos > 0 ? doc.textBetween(pos - 1, pos, '') : '';
            
            if (char2 === '[' && char1 === '[') {
                bracketPos = pos - 1; // Position of first [
                break;
            }
        } catch (e) {
            // Position might be at a node boundary, skip
            continue;
        }
    }
    
    if (bracketPos === -1) {
        console.error('Could not find [[ before cursor');
        return;
    }
    
    // Use editor chain for cleaner manipulation
    editor
        .chain()
        .focus()
        .deleteRange({ from: bracketPos, to: from })
        .insertContent({
            type: 'wikiLink',
            attrs: {
                targetArticleId: articleId,
                displayText: finalDisplayText,
                broken: false
            }
        })
        .insertContent(' ')
        .run();
}

/**
 * Insert an external link token at the current cursor position
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

    // Check if user provided custom display text via pipe syntax
    const customDisplayText = window._wikiLinkCustomDisplayText;
    const finalTitle = customDisplayText && customDisplayText.trim() ? customDisplayText.trim() : title;

    // Clear the stored custom display text
    window._wikiLinkCustomDisplayText = null;

    const { from } = editor.state.selection;

    // Search backwards from cursor to find [[
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
        .insertContent({
            type: 'externalLink',
            attrs: {
                source: source,
                externalId: id,
                title: finalTitle
            }
        })
        .insertContent(' ')
        .run();
}

// Make functions globally available
window.initializeWikiLinkAutocomplete = initializeWikiLinkAutocomplete;
window.insertWikiLink = insertWikiLink;
window.insertExternalLinkToken = insertExternalLinkToken;

/**
 * Update the autocomplete text (for category selection)
 * Replaces the current [[... text with the new text
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

    // Search backwards from cursor to find [[
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

    // Replace [[ to cursor with [[newText
    editor
        .chain()
        .focus()
        .deleteRange({ from: bracketPos, to: from })
        .insertContent('[[' + newText)
        .run();
}

window.updateAutocompleteText = updateAutocompleteText;

/**
 * Insert multiple wiki links at specified HTML positions.
 * Converts HTML character positions to TipTap document positions and inserts links.
 * 
 * @param {string} editorId - The editor container ID
 * @param {Array} matches - Array of {articleId, displayText, startIndex, endIndex}
 */
function insertWikiLinksAtPositions(editorId, matches) {
    const editor = window.tipTapEditors[editorId];
    if (!editor) {
        console.error('Editor not found:', editorId);
        return;
    }

    if (!matches || matches.length === 0) {
        console.log('No matches to insert');
        return;
    }

    // Get the current HTML content
    const currentHtml = editor.getHTML();
    
    // Sort matches by startIndex descending so we process from end to start
    // This preserves earlier positions when we make replacements
    const sortedMatches = [...matches].sort((a, b) => b.startIndex - a.startIndex);
    
    // Build new HTML with wiki link spans inserted
    let newHtml = currentHtml;
    
    for (const match of sortedMatches) {
        const { articleId, displayText, startIndex, endIndex } = match;
        
        // Verify the text at this position matches what we expect
        const textAtPosition = newHtml.substring(startIndex, endIndex);
        if (textAtPosition.toLowerCase() !== displayText.toLowerCase()) {
            console.warn(`Text mismatch at position ${startIndex}-${endIndex}: expected "${displayText}", found "${textAtPosition}"`);
            continue;
        }
        
        // Create the wiki link span HTML
        const wikiLinkHtml = `<span data-type="wiki-link" class="wiki-link-node" data-target-id="${articleId}" data-display="${escapeHtmlAttr(displayText)}">${escapeHtml(displayText)}</span>`;
        
        // Replace the text with the wiki link span
        newHtml = newHtml.substring(0, startIndex) + wikiLinkHtml + newHtml.substring(endIndex);
    }
    
    // Set the new content in the editor
    editor.commands.setContent(newHtml);
    
    console.log(`Inserted ${matches.length} wiki links`);
}

/**
 * Escape HTML special characters for use in HTML content
 */
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

/**
 * Escape text for use in HTML attributes
 */
function escapeHtmlAttr(text) {
    return text
        .replace(/&/g, '&amp;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;');
}

/**
 * Get the current HTML content from the editor
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

// Make functions globally available
window.insertWikiLinksAtPositions = insertWikiLinksAtPositions;
window.getTipTapContent = getTipTapContent;
