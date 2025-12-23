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
            
            // Notify Blazor to show autocomplete (only search on the part before pipe)
            dotNetHelper.invokeMethodAsync('OnAutocompleteTriggered', searchQuery, coords.left, coords.bottom);
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

    console.log('✅ Wiki link autocomplete initialized for', editorId);
}

/**
 * Insert a wiki link at the current cursor position
 * @param {string} editorId - The editor container ID
 * @param {string} articleId - The GUID of the article to link
 * @param {string} displayText - The text to display
 */
function insertWikiLink(editorId, articleId, displayText) {
    console.log('🔵 insertWikiLink called:', { editorId, articleId, displayText });
    
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
    
    console.log('🔵 Using display text:', finalDisplayText, '(custom:', customDisplayText, ')');

    const { from } = editor.state.selection;
    console.log('🔵 Cursor position:', from);
    
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
        console.error('❌ Could not find [[ before cursor');
        return;
    }

    console.log('🔵 Found [[ at position:', bracketPos);
    console.log('🔵 Deleting from', bracketPos, 'to', from);
    
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

    console.log('✅ Wiki link inserted:', articleId, finalDisplayText);
}

// Make functions globally available
window.initializeWikiLinkAutocomplete = initializeWikiLinkAutocomplete;
window.insertWikiLink = insertWikiLink;

console.log('✅ Wiki link autocomplete script loaded');
