// ================================================
// Image Upload Integration for TipTap Editor
// ================================================
// Handles image uploads via drag-drop, paste, and
// toolbar button. Uploads to Azure Blob Storage via
// the existing WorldDocument SAS URL flow, then
// inserts an <img> tag pointing to the proxy endpoint.
// ================================================

// Allowed image MIME types
const ALLOWED_IMAGE_TYPES = ['image/png', 'image/jpeg', 'image/gif', 'image/webp'];
const MAX_IMAGE_SIZE = 10 * 1024 * 1024; // 10 MB for inline images

/**
 * Initialize image upload handling for a TipTap editor instance.
 * @param {string} editorId - The DOM container ID for the editor
 * @param {object} dotNetHelper - Blazor DotNetObjectReference for interop
 */
window.initializeImageUpload = function (editorId, dotNetHelper) {
    const container = document.getElementById(editorId);
    if (!container) return;

    // Handle paste events with images
    container.addEventListener('paste', async (e) => {
        const items = e.clipboardData?.items;
        if (!items) return;

        for (const item of items) {
            if (ALLOWED_IMAGE_TYPES.includes(item.type)) {
                e.preventDefault();
                const file = item.getAsFile();
                if (file) {
                    await uploadAndInsertImage(editorId, dotNetHelper, file);
                }
                return;
            }
        }
    });

    // Handle drag-and-drop images
    container.addEventListener('drop', async (e) => {
        const files = e.dataTransfer?.files;
        if (!files || files.length === 0) return;

        for (const file of files) {
            if (ALLOWED_IMAGE_TYPES.includes(file.type)) {
                e.preventDefault();
                await uploadAndInsertImage(editorId, dotNetHelper, file);
                return; // Only handle first image
            }
        }
    });

    // Prevent default drag behavior to enable drop
    container.addEventListener('dragover', (e) => {
        if (e.dataTransfer?.types?.includes('Files')) {
            e.preventDefault();
        }
    });
};

/**
 * Trigger a file picker for image upload from toolbar button.
 * @param {string} editorId - The DOM container ID for the editor
 * @param {object} dotNetHelper - Blazor DotNetObjectReference for interop
 */
window.triggerImageUpload = function (editorId, dotNetHelper) {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/png,image/jpeg,image/gif,image/webp';
    input.style.display = 'none';

    input.addEventListener('change', async () => {
        const file = input.files?.[0];
        if (file) {
            await uploadAndInsertImage(editorId, dotNetHelper, file);
        }
        input.remove();
    });

    document.body.appendChild(input);
    input.click();
};

/**
 * Core upload function: validates, shows placeholder, uploads to blob, inserts final image.
 */
async function uploadAndInsertImage(editorId, dotNetHelper, file) {
    // Validate file type
    if (!ALLOWED_IMAGE_TYPES.includes(file.type)) {
        await dotNetHelper.invokeMethodAsync('OnImageUploadError', 'Unsupported image type. Use PNG, JPEG, GIF, or WebP.');
        return;
    }

    // Validate file size
    if (file.size > MAX_IMAGE_SIZE) {
        await dotNetHelper.invokeMethodAsync('OnImageUploadError', `Image too large (${formatBytes(file.size)}). Maximum size is 10 MB.`);
        return;
    }

    const editor = window.tipTapEditors[editorId];
    if (!editor) {
        console.error('Editor not found:', editorId);
        return;
    }

    // Generate a unique placeholder ID
    const placeholderId = 'img-upload-' + crypto.randomUUID();

    // Insert a loading placeholder at current cursor position
    editor.chain().focus().insertContent(
        `<img src="data:image/svg+xml,${encodeURIComponent(createPlaceholderSvg())}" alt="Uploading..." data-upload-id="${placeholderId}" class="chronicis-image-uploading" />`
    ).run();

    try {
        // Read file as byte array
        const arrayBuffer = await file.arrayBuffer();
        const bytes = new Uint8Array(arrayBuffer);

        // Call Blazor to request upload SAS URL
        const uploadResponse = await dotNetHelper.invokeMethodAsync(
            'OnImageUploadRequested',
            file.name,
            file.type,
            file.size
        );

        if (!uploadResponse || !uploadResponse.uploadUrl) {
            throw new Error('Failed to get upload URL');
        }

        // Upload directly to blob storage via SAS URL
        const response = await fetch(uploadResponse.uploadUrl, {
            method: 'PUT',
            headers: {
                'Content-Type': file.type,
                'x-ms-blob-type': 'BlockBlob'
            },
            body: bytes
        });

        if (!response.ok) {
            throw new Error(`Blob upload failed: ${response.status}`);
        }

        // Confirm upload with the API
        await dotNetHelper.invokeMethodAsync(
            'OnImageUploadConfirmed',
            uploadResponse.documentId
        );

        // Build the proxy URL for this image
        const proxyUrl = await dotNetHelper.invokeMethodAsync(
            'GetImageProxyUrl',
            uploadResponse.documentId
        );

        // Replace the placeholder with the real image
        replacePlaceholder(editor, placeholderId, proxyUrl, file.name);

        // Trigger editor update so Blazor gets the new HTML
        const html = editor.getHTML();
        await dotNetHelper.invokeMethodAsync('OnEditorUpdate', html);

    } catch (err) {
        console.error('Image upload failed:', err);
        // Remove the placeholder on failure
        removePlaceholder(editor, placeholderId);
        await dotNetHelper.invokeMethodAsync('OnImageUploadError', `Upload failed: ${err.message}`);
    }
}

/**
 * Replace a placeholder image with the final uploaded image URL.
 */
function replacePlaceholder(editor, placeholderId, imageUrl, altText) {
    const container = editor.view.dom;
    const placeholder = container.querySelector(`img[data-upload-id="${placeholderId}"]`);

    if (placeholder) {
        placeholder.src = imageUrl;
        placeholder.alt = altText || 'Uploaded image';
        placeholder.removeAttribute('data-upload-id');
        placeholder.classList.remove('chronicis-image-uploading');
    }
}

/**
 * Remove a placeholder image (on upload failure).
 */
function removePlaceholder(editor, placeholderId) {
    const container = editor.view.dom;
    const placeholder = container.querySelector(`img[data-upload-id="${placeholderId}"]`);
    if (placeholder) {
        placeholder.remove();
    }
}

/**
 * Create an SVG placeholder for the uploading state.
 */
function createPlaceholderSvg() {
    return `<svg xmlns="http://www.w3.org/2000/svg" width="300" height="100" viewBox="0 0 300 100">
        <rect width="300" height="100" rx="8" fill="#f0ece4" stroke="#c4af8e" stroke-width="2" stroke-dasharray="8 4"/>
        <text x="150" y="50" text-anchor="middle" dominant-baseline="middle" font-family="sans-serif" font-size="14" fill="#3a4750">
            Uploading image…
        </text>
    </svg>`;
}

function formatBytes(bytes) {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
}
