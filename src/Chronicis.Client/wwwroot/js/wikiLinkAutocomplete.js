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

}
