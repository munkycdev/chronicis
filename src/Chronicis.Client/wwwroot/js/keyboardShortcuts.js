// Global keyboard shortcuts for Chronicis

window.chronicisKeyboardShortcuts = {
    dotNetHelper: null,
    keyDownHandler: null,
    
    initialize: function(dotNetHelper) {
        // Prevent duplicate initialization
        if (this.dotNetHelper) {
            console.warn('Keyboard shortcuts already initialized');
            return;
        }
        
        this.dotNetHelper = dotNetHelper;
        
        // Store bound handler for proper cleanup
        this.keyDownHandler = this.handleKeyDown.bind(this);
        document.addEventListener('keydown', this.keyDownHandler);
        
        // Register global quest drawer close function for TipTap editor
        window.chronicisCloseQuestDrawer = () => {
            if (this.dotNetHelper) {
                this.dotNetHelper.invokeMethodAsync('OnCtrlQ');
            }
        };
    },
    
    dispose: function() {
        if (this.keyDownHandler) {
            document.removeEventListener('keydown', this.keyDownHandler);
            this.keyDownHandler = null;
        }
        this.dotNetHelper = null;
        window.chronicisCloseQuestDrawer = null;
    },
    
    handleKeyDown: function(e) {
        // Skip if user is typing in an input field (unless it's the tree view)
        const activeElement = document.activeElement;
        const isInInput = activeElement && (
            activeElement.tagName === 'INPUT' ||
            activeElement.tagName === 'TEXTAREA' ||
            activeElement.isContentEditable ||
            activeElement.closest('.ProseMirror') // TipTap editor
        );
        
        // Ctrl+S - Save current article (works everywhere including editor)
        if (e.ctrlKey && e.key === 's') {
            e.preventDefault();
            if (this.dotNetHelper) {
                this.dotNetHelper.invokeMethodAsync('OnCtrlS');
            }
            return;
        }
        
        // Ctrl+N - Create new sibling article (works everywhere)
        if (e.ctrlKey && e.key === 'n') {
            e.preventDefault();
            if (this.dotNetHelper) {
                this.dotNetHelper.invokeMethodAsync('OnCtrlN');
            }
            return;
        }
        
        // Ctrl+M - Toggle metadata drawer (works everywhere)
        if (e.ctrlKey && e.key === 'm') {
            e.preventDefault();
            if (this.dotNetHelper) {
                this.dotNetHelper.invokeMethodAsync('OnCtrlM');
            }
            return;
        }
        
        // Ctrl+Q - Toggle quest drawer (works everywhere)
        if (e.ctrlKey && e.key === 'q') {
            e.preventDefault();
            if (this.dotNetHelper) {
                this.dotNetHelper.invokeMethodAsync('OnCtrlQ');
            }
            return;
        }

        // Ctrl+T - Toggle tutorial drawer (works everywhere, including editors).
        // Do not preventDefault so browser/platform-reserved Ctrl+T can still win gracefully.
        if (e.ctrlKey && e.key && e.key.toLowerCase() === 't') {
            if (this.dotNetHelper) {
                this.dotNetHelper.invokeMethodAsync('OnCtrlT');
            }
            return;
        }
        
        // Skip other shortcuts if in input
        if (isInInput) {
            return;
        }
        
        // Future shortcuts can be added here
    }
};

// ================================================
// File Download Utility
// ================================================

/**
 * Trigger a file download in the browser
 * @param {string} fileName - Name for the downloaded file
 * @param {string} contentType - MIME type of the file
 * @param {Uint8Array} content - File content as byte array
 */
window.chronicisDownloadFile = function(fileName, contentType, content) {
    // Create a blob from the byte array
    const blob = new Blob([content], { type: contentType });
    
    // Create a temporary URL for the blob
    const url = URL.createObjectURL(blob);
    
    // Create a temporary anchor element and trigger download
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    
    // Clean up
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};

// ================================================
// File Open Utility (New Tab)
// ================================================

window.chronicisDocumentTab = {
    lastWindow: null
};

/**
 * Open a placeholder tab so pop-up blockers allow the window.
 */
window.chronicisOpenDocumentTab = function() {
    const docWindow = window.open("", "_blank");
    if (docWindow) {
        docWindow.document.title = "Loading document...";
        docWindow.document.body.innerHTML = "<p style=\"font-family: sans-serif; padding: 16px;\">Loading document...</p>";
    }
    window.chronicisDocumentTab.lastWindow = docWindow;
};

/**
 * Load a file into the most recently opened tab (or open a new one).
 * @param {string} fileName - Name for the document
 * @param {string} contentType - MIME type of the file
 * @param {Uint8Array} content - File content as byte array
 */
window.chronicisLoadDocumentInTab = function(fileName, contentType, content) {
    const blob = new Blob([content], { type: contentType });
    const url = URL.createObjectURL(blob);
    const docWindow = window.chronicisDocumentTab.lastWindow;

    if (docWindow && !docWindow.closed) {
        docWindow.location = url;
        try {
            docWindow.document.title = fileName;
        } catch {
            // Ignore cross-origin updates if browser blocks access.
        }
    } else {
        window.open(url, "_blank");
    }

    setTimeout(() => URL.revokeObjectURL(url), 60000);
};
