// Global keyboard shortcuts for Chronicis

window.chronicisKeyboardShortcuts = {
    dotNetHelper: null,
    
    initialize: function(dotNetHelper) {
        this.dotNetHelper = dotNetHelper;
        
        document.addEventListener('keydown', this.handleKeyDown.bind(this));
    },
    
    dispose: function() {
        document.removeEventListener('keydown', this.handleKeyDown.bind(this));
        this.dotNetHelper = null;
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
        
        // Ctrl+N - Create new sibling article
        if (e.ctrlKey && e.key === 'n') {
            // Allow in inputs too for this shortcut - user explicitly wants new article
            e.preventDefault();
            if (this.dotNetHelper) {
                this.dotNetHelper.invokeMethodAsync('OnCtrlN');
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
