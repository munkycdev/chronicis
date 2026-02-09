// questEditor.js - TipTap editor for quest updates
// Uses global window.TipTap loaded from CDN

let editor = null;
let dotNetRef = null;
let isInitializing = false;

export async function initializeEditor(elementId, dotNetRefParam) {
    // Prevent duplicate initialization
    if (isInitializing) {
        console.warn('Quest editor initialization already in progress');
        return;
    }
    
    // Wait for element to be available in DOM (with retry)
    let element = document.getElementById(elementId);
    let retries = 0;
    const maxRetries = 10;
    
    while (!element && retries < maxRetries) {
        console.log(`Quest editor element not found, waiting... (attempt ${retries + 1}/${maxRetries})`);
        await new Promise(resolve => setTimeout(resolve, 100));
        element = document.getElementById(elementId);
        retries++;
    }
    
    if (!element) {
        console.error('Quest editor element not found after retries:', elementId);
        return;
    }
    
    // Check if TipTap is loaded
    if (!window.TipTap || !window.TipTap.Editor) {
        console.error('TipTap not loaded yet, waiting...');
        await new Promise(resolve => {
            window.addEventListener('tiptap-ready', resolve, { once: true });
        });
    }
    
    // Store the dotNetRef for Esc key handling
    dotNetRef = dotNetRefParam;
    
    // Destroy existing editor if any
    if (editor) {
        editor.destroy();
        editor = null;
    }
    
    isInitializing = true;
    
    try {
        // Build extensions array
        const extensions = [
            window.TipTap.StarterKit.configure({
                heading: {
                    levels: [1, 2, 3]
                }
            })
        ];
        
        // Add wiki link extension if available
        if (window.createWikiLinkExtension) {
            try {
                const wikiLinkExt = window.createWikiLinkExtension();
                if (wikiLinkExt) {
                    extensions.push(wikiLinkExt);
                    console.log('Wiki link extension added to quest editor');
                }
            } catch (err) {
                console.error('Failed to load wiki link extension in quest editor:', err);
            }
        }
        
        // Add external link extension if available
        if (window.createExternalLinkExtension) {
            try {
                const externalLinkExt = window.createExternalLinkExtension();
                if (externalLinkExt) {
                    extensions.push(externalLinkExt);
                    console.log('External link extension added to quest editor');
                }
            } catch (err) {
                console.error('Failed to load external link extension in quest editor:', err);
            }
        }
        
        editor = new window.TipTap.Editor({
            element: element,
            extensions: extensions,
            content: '',
            editorProps: {
                attributes: {
                    class: 'chronicis-editor-content',
                    'data-placeholder': 'Add an update about this quest...'
                },
                handleKeyDown: (view, event) => {
                    // Handle Esc key to close drawer
                    if (event.key === 'Escape') {
                        event.preventDefault();
                        event.stopPropagation();
                        
                        // Invoke the close handler on the QuestDrawer component
                        if (dotNetRef) {
                            try {
                                // Close the drawer via the service
                                window.chronicisCloseQuestDrawer?.();
                            } catch (err) {
                                console.error('Failed to close quest drawer:', err);
                            }
                        }
                        
                        return true;
                    }
                    return false;
                }
            }
        });
        
        // Store editor instance for autocomplete
        window.tipTapEditors = window.tipTapEditors || {};
        window.tipTapEditors[elementId] = editor;
        
        // Initialize wiki link autocomplete
        if (typeof initializeWikiLinkAutocomplete === 'function') {
            initializeWikiLinkAutocomplete(elementId, dotNetRef);
            console.log('Wiki link autocomplete initialized for quest editor');
        } else {
            console.warn('initializeWikiLinkAutocomplete not available');
        }
        
        console.log('Quest editor initialized successfully with', extensions.length, 'extensions');
    } catch (error) {
        console.error('Error creating TipTap editor:', error);
        throw error;
    } finally {
        isInitializing = false;
    }
}

export function destroyEditor() {
    if (editor) {
        // Get the element ID before destroying
        const elementId = 'quest-update-editor';
        
        // Remove from global store
        if (window.tipTapEditors && window.tipTapEditors[elementId]) {
            delete window.tipTapEditors[elementId];
        }
        
        editor.destroy();
        editor = null;
    }
    dotNetRef = null;
    isInitializing = false;
}

export function getEditorContent() {
    if (!editor) {
        return '';
    }
    
    return editor.getHTML();
}

export function clearEditor() {
    if (editor) {
        editor.commands.setContent('');
        editor.commands.focus();
    }
}

export function focusEditor() {
    if (editor) {
        editor.commands.focus();
    }
}

export function insertWikiLink(linkText, customDisplayText) {
    if (!editor) {
        console.error('Cannot insert wiki link: editor not initialized');
        return;
    }
    
    try {
        // Get current selection position
        const { from } = editor.state.selection;
        
        // Find the [[ that triggered the autocomplete
        const textBefore = editor.state.doc.textBetween(Math.max(0, from - 100), from, '\n');
        const match = textBefore.match(/\[\[([^\]]*)$/);
        
        if (match) {
            const matchLength = match[0].length; // Length of "[[query"
            const deleteFrom = from - matchLength;
            
            // Delete the [[ and query text
            editor.chain()
                .focus()
                .deleteRange({ from: deleteFrom, to: from })
                .run();
                
            // Insert the wiki link
            const displayText = customDisplayText || linkText;
            const fullLinkText = customDisplayText ? `${linkText}|${customDisplayText}` : linkText;
            
            editor.chain()
                .focus()
                .insertContent([
                    { type: 'text', text: '[[' },
                    { type: 'text', text: fullLinkText },
                    { type: 'text', text: ']]' },
                    { type: 'text', text: ' ' } // Add space after
                ])
                .run();
        }
    } catch (err) {
        console.error('Error inserting wiki link:', err);
    }
}

