/**
 * Emoji Picker Interop for Chronicis
 * Integrates Picmo picker with Blazor components
 */

// Store active picker instances by container ID
const activePickers = new Map();

/**
 * Wait for Picmo to be loaded
 */
async function waitForPicmo() {
    if (window.Picmo) {
        return window.Picmo;
    }
    
    return new Promise((resolve) => {
        window.addEventListener('picmo-ready', () => {
            resolve(window.Picmo);
        }, { once: true });
        
        // Timeout fallback
        setTimeout(() => {
            if (window.Picmo) {
                resolve(window.Picmo);
            } else {
                console.error('Picmo failed to load');
                resolve(null);
            }
        }, 5000);
    });
}

/**
 * Initialize an emoji picker in the specified container
 * @param {string} containerId - The DOM element ID to attach the picker to
 * @param {object} dotNetHelper - Blazor .NET object reference for callbacks
 */
window.initializeEmojiPicker = async function (containerId, dotNetHelper) {
    const container = document.getElementById(containerId);
    if (!container) {
        console.error(`Emoji picker container not found: ${containerId}`);
        return;
    }

    // Wait for Picmo to load
    const Picmo = await waitForPicmo();
    if (!Picmo) {
        console.error('Picmo not available');
        return;
    }

    // Clean up existing picker if any
    if (activePickers.has(containerId)) {
        destroyEmojiPicker(containerId);
    }

    try {
        // Create the picker
        const picker = Picmo.createPicker({
            rootElement: container,
            theme: 'light',
            showPreview: false,
            showSearch: true,
            showCategoryTabs: true,
            showRecents: true,
            emojisPerRow: 8,
            emojiSize: '1.5rem'
        });

        // Listen for emoji selection
        picker.addEventListener('emoji:select', (event) => {
            console.log('Picmo emoji selected:', event);
            const emoji = event.emoji;
            console.log('Emoji character:', emoji);
            
            if (emoji && dotNetHelper) {
                dotNetHelper.invokeMethodAsync('OnEmojiSelected', emoji)
                    .then(() => console.log('Blazor callback succeeded'))
                    .catch(err => console.error('Blazor callback failed:', err));
            }
        });

        activePickers.set(containerId, { picker, dotNetHelper });
        console.log('Picmo picker initialized in container:', containerId);
    } catch (err) {
        console.error('Failed to create Picmo picker:', err);
    }
};

/**
 * Destroy an emoji picker instance
 * @param {string} containerId - The DOM element ID of the picker container
 */
window.destroyEmojiPicker = function (containerId) {
    if (activePickers.has(containerId)) {
        const { picker } = activePickers.get(containerId);
        if (picker && picker.destroy) {
            picker.destroy();
        }
        activePickers.delete(containerId);
    }
    
    const container = document.getElementById(containerId);
    if (container) {
        container.innerHTML = '';
    }
};

/**
 * Show/hide the emoji picker container
 * @param {string} containerId - The DOM element ID of the picker container
 * @param {boolean} show - Whether to show or hide
 */
window.toggleEmojiPicker = function (containerId, show) {
    const container = document.getElementById(containerId);
    if (container) {
        container.style.display = show ? 'block' : 'none';
    }
};
