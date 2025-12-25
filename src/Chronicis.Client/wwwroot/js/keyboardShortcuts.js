// Global keyboard shortcuts for Chronicis

window.chronicisKeyboardShortcuts = {
    dotNetHelper: null,
    
    initialize: function(dotNetHelper) {
        this.dotNetHelper = dotNetHelper;
        
        document.addEventListener('keydown', this.handleKeyDown.bind(this));
        console.log('Chronicis keyboard shortcuts initialized');
    },
    
    dispose: function() {
        document.removeEventListener('keydown', this.handleKeyDown.bind(this));
        this.dotNetHelper = null;
        console.log('Chronicis keyboard shortcuts disposed');
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
