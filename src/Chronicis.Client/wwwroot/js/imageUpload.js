// ================================================
// Image Upload Integration for TipTap Editor
// ================================================
// Handles image uploads via drag-drop, paste, and
// toolbar button. Uploads to Azure Blob Storage via
// the existing WorldDocument SAS URL flow, then
// inserts an <img> with a stable chronicis-image:
// reference that gets resolved to a SAS URL on render.
// ================================================

const ALLOWED_IMAGE_TYPES = ['image/png', 'image/jpeg', 'image/gif', 'image/webp'];
const MAX_IMAGE_SIZE = 10 * 1024 * 1024; // 10 MB
const IMAGE_SRC_PREFIX = 'chronicis-image:';

// Cache resolved SAS URLs to avoid redundant API calls during the same session
const resolvedUrlCache = new Map();

/**
 * Initialize image upload handling for a TipTap editor instance.
 */
window.initializeImageUpload = function (editorId, dotNetHelper) {
    const container = document.getElementById(editorId);
    if (!container) return;

    container.addEventListener('paste', async (e) => {
        const items = e.clipboardData?.items;
        if (!items) return;

        for (const item of items) {
            if (ALLOWED_IMAGE_TYPES.includes(item.type)) {
                e.preventDefault();
                e.stopPropagation();
                const file = item.getAsFile();
                if (file) await uploadAndInsertImage(editorId, dotNetHelper, file);
                return;
            }
        }
    });

    container.addEventListener('drop', async (e) => {
        const files = e.dataTransfer?.files;
        if (!files || files.length === 0) return;

        for (const file of files) {
            if (ALLOWED_IMAGE_TYPES.includes(file.type)) {
                e.preventDefault();
                e.stopPropagation();
                await uploadAndInsertImage(editorId, dotNetHelper, file);
                return;
            }
        }
    });

    container.addEventListener('dragover', (e) => {
        if (e.dataTransfer?.types?.includes('Files')) {
            e.preventDefault();
        }
    });
};

/**
 * Trigger a file picker for image upload from toolbar button.
 */
window.triggerImageUpload = function (editorId, dotNetHelper) {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/png,image/jpeg,image/gif,image/webp';
    input.style.display = 'none';

    input.addEventListener('change', async () => {
        const file = input.files?.[0];
        if (file) await uploadAndInsertImage(editorId, dotNetHelper, file);
        input.remove();
    });

    document.body.appendChild(input);
    input.click();
};

/**
 * Resolve all chronicis-image: src references in the editor to real SAS URLs.
 * Called after editor initialization and after content is set.
 */
window.resolveEditorImages = async function (editorId, dotNetHelper) {
    const container = document.getElementById(editorId);
    if (!container) return;

    const images = container.querySelectorAll(`img[src^="${IMAGE_SRC_PREFIX}"]`);
    
    for (const img of images) {
        const documentId = img.src.replace(IMAGE_SRC_PREFIX, '');
        if (!documentId) continue;

        // Check cache first
        if (resolvedUrlCache.has(documentId)) {
            img.src = resolvedUrlCache.get(documentId);
            continue;
        }

        try {
            const sasUrl = await dotNetHelper.invokeMethodAsync('ResolveImageUrl', documentId);
            if (sasUrl) {
                resolvedUrlCache.set(documentId, sasUrl);
                img.src = sasUrl;
            }
        } catch (err) {
            console.error('Failed to resolve image:', documentId, err);
        }
    }
};

/**
 * Upload image to blob storage, then insert via TipTap's setImage command.
 */
async function uploadAndInsertImage(editorId, dotNetHelper, file) {
    if (!ALLOWED_IMAGE_TYPES.includes(file.type)) {
        await dotNetHelper.invokeMethodAsync('OnImageUploadError', 'Unsupported image type. Use PNG, JPEG, GIF, or WebP.');
        return;
    }

    if (file.size > MAX_IMAGE_SIZE) {
        await dotNetHelper.invokeMethodAsync('OnImageUploadError',
            `Image too large (${formatBytes(file.size)}). Maximum size is 10 MB.`);
        return;
    }

    const editor = window.tipTapEditors[editorId];
    if (!editor) return;

    await dotNetHelper.invokeMethodAsync('OnImageUploadStarted', file.name);

    try {
        const bytes = new Uint8Array(await file.arrayBuffer());

        // Request SAS upload URL from API
        const uploadResponse = await dotNetHelper.invokeMethodAsync(
            'OnImageUploadRequested', file.name, file.type, file.size);

        if (!uploadResponse?.uploadUrl) throw new Error('Failed to get upload URL');

        // Upload to blob storage
        const response = await fetch(uploadResponse.uploadUrl, {
            method: 'PUT',
            headers: { 'Content-Type': file.type, 'x-ms-blob-type': 'BlockBlob' },
            body: bytes
        });

        if (!response.ok) throw new Error(`Upload failed: ${response.status}`);

        // Confirm upload
        await dotNetHelper.invokeMethodAsync('OnImageUploadConfirmed', uploadResponse.documentId);

        // Insert with stable chronicis-image: reference
        const stableSrc = IMAGE_SRC_PREFIX + uploadResponse.documentId;
        editor.chain().focus().setImage({ src: stableSrc, alt: file.name }).run();

        // Immediately resolve it to a real URL for display
        await window.resolveEditorImages(editorId, dotNetHelper);

        // Trigger editor update so Blazor gets the HTML with the stable reference
        const html = editor.getHTML();
        await dotNetHelper.invokeMethodAsync('OnEditorUpdate', html);

    } catch (err) {
        console.error('Image upload failed:', err);
        await dotNetHelper.invokeMethodAsync('OnImageUploadError', `Upload failed: ${err.message}`);
    }
}

function formatBytes(bytes) {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
}
