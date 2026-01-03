# Document Storage - User Guide

## Overview

Chronicis now supports uploading and managing documents for your campaigns! Upload PDFs, Office files, images, and more to keep all your campaign resources in one place.

## Supported File Types

- **Documents:** PDF (.pdf), Word (.docx), Text (.txt), Markdown (.md)
- **Spreadsheets:** Excel (.xlsx)
- **Presentations:** PowerPoint (.pptx)
- **Images:** PNG, JPEG, GIF, WebP

**Maximum file size:** 200 MB per file

---

## Uploading Documents

### From World Detail Page

1. Navigate to your world's detail page
2. Scroll to the **Documents** section
3. Click the **"Upload Document"** button
4. Select your file from the file picker
5. Wait for upload to complete
6. Your document will appear in the list

### Upload Dialog Features

- **Drag & Drop:** You can drag files directly into the upload dialog
- **File Validation:** Invalid files are rejected with clear error messages
- **Progress Indicator:** See real-time upload progress
- **Auto-Rename:** Duplicate filenames are automatically renamed (e.g., "Map.pdf" → "Map (2).pdf")

---

## Viewing Documents

### In Navigation Tree

Documents appear under **"External Resources"** in the left navigation panel alongside your external links.

- Each document shows its file type icon
- Click any document to download it

### On World Detail Page

The **Documents** section shows all uploaded documents with:

- **Title:** The document name
- **Description:** Optional description (editable by GM)
- **File Info:** File size and upload date
- **Actions:** Download, Edit, Delete (GM only)

---

## Downloading Documents

### Quick Download

1. Click on document name in navigation tree, OR
2. Click the download icon in the Documents section

The file will begin downloading immediately through your browser.

**Note:** Download links expire after 15 minutes for security. If a link expires, simply click download again to generate a new link.

---

## Managing Documents (GM Only)

### Edit Document Title or Description

1. Navigate to World Detail page
2. Find the document in the Documents section
3. Click the **Edit** icon (pencil)
4. Modify the title and/or description
5. Click the **checkmark** to save, or **X** to cancel

**Tips:**
- Titles help you identify documents at a glance
- Descriptions can provide context (e.g., "Map of Waterdeep, Session 5")
- Changes update immediately in both the list and navigation tree

### Delete Document

1. Navigate to World Detail page
2. Find the document in the Documents section
3. Click the **Delete** icon (trash can)
4. Confirm deletion in the dialog

**Warning:** Deleting a document is permanent and cannot be undone. The file will be removed from storage.

---

## Permissions

### Game Master (GM)
- Upload documents
- Edit document titles/descriptions
- Delete documents
- Download documents

### Players
- Download documents
- View document list

**Note:** Only the world owner (GM) can upload, edit, or delete documents. All world members can view and download.

---

## Best Practices

### Organizing Documents

- **Use clear titles:** "Session 3 - Waterdeep Maps" is better than "maps.pdf"
- **Add descriptions:** Help players understand what the document contains
- **Group by category:** Use naming conventions like "Map - ", "NPC - ", "Handout - "

### File Size Tips

- **Optimize PDFs:** Use PDF compression to reduce file sizes
- **Resize images:** Large images can be resized before upload
- **Split large files:** Break huge documents into smaller sections if needed

### Security

- Documents are stored securely in Azure Blob Storage
- Download links are temporary (15-minute expiration)
- Only world members can access documents
- Deleted documents are permanently removed from storage

---

## Common Questions

### Q: Can I upload the same file twice?

A: Yes! If you upload a file with the same name, it will be automatically renamed (e.g., "Document (2).pdf").

### Q: What happens if I delete a document?

A: The document is permanently removed from both the database and blob storage. This cannot be undone.

### Q: Can players upload documents?

A: No, only GMs can upload, edit, and delete documents. Players can view and download.

### Q: Why did my download link expire?

A: For security, download links expire after 15 minutes. Simply click download again to get a new link.

### Q: Can I organize documents into folders?

A: Not yet! Currently, all documents are in a flat list. You can use naming conventions (prefixes) to organize them.

### Q: How do I upload multiple files at once?

A: Currently, files must be uploaded one at a time. Select a file, wait for upload to complete, then upload the next one.

### Q: What's the maximum file size?

A: 200 MB per file. If you need to share larger files, consider using external links instead.

---

## Troubleshooting

### Upload Fails

**Problem:** "File type not allowed"
- **Solution:** Check that your file type is supported (PDF, DOCX, XLSX, PPTX, TXT, MD, images)

**Problem:** "File size exceeds maximum"
- **Solution:** Reduce file size or split into smaller files (max 200 MB)

**Problem:** Upload gets stuck
- **Solution:** Check your internet connection. Close the dialog and try again.

### Download Issues

**Problem:** Download link doesn't work
- **Solution:** The link may have expired (15 min). Generate a new link by clicking download again.

**Problem:** File won't open
- **Solution:** Ensure you have software installed to open the file type (e.g., PDF reader, Microsoft Office)

### Display Issues

**Problem:** Document doesn't appear after upload
- **Solution:** Refresh the page. If still missing, the upload may have failed.

**Problem:** Wrong icon displayed
- **Solution:** This is cosmetic. The correct file will still download.

---

## Tips & Tricks

1. **Session Handouts:** Upload character portraits, maps, and handouts before sessions for easy sharing
2. **Reference Materials:** Keep SRD excerpts, spell cards, and rule clarifications handy
3. **Campaign Assets:** Store world maps, faction logos, and custom content
4. **Player Resources:** Share character sheets, spell lists, and equipment trackers
5. **Archive:** Keep a document history of major campaign events and decisions

---

## Feature Roadmap

**Coming Soon:**
- Folder organization
- Batch upload
- Document preview
- Version history
- Sharing outside your world

---

## Need Help?

If you encounter issues or have feature requests:

1. Check this guide first
2. Report bugs via the feedback button
3. Request features in the Chronicis Discord/forum

---

**Happy chronicling!** 📚✨
