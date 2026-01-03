# Document Storage Feature - Testing Guide

## Overview
This guide covers manual testing procedures for the Document Storage feature in Chronicis.

## Test Environment Setup
1. Ensure you have a test world created
2. Ensure you have GM permissions on the test world
3. Have test files ready:
   - PDF file (< 200MB)
   - Word document (.docx)
   - Excel spreadsheet (.xlsx)
   - PowerPoint presentation (.pptx)
   - Text file (.txt)
   - Markdown file (.md)
   - Image file (.png, .jpg, etc.)
   - Large file (> 200MB for validation testing)
   - File with special characters in name

## Feature Test Scenarios

### 1. Upload Document (Happy Path)

**Test Steps:**
1. Navigate to World Detail page
2. Scroll to "Documents" section
3. Click "Upload Document" button
4. Select a valid PDF file (< 200MB)
5. Click "Upload" button

**Expected Results:**
- ✅ Upload dialog opens
- ✅ File picker allows selection
- ✅ Progress indicator shows during upload
- ✅ Success message: "Document uploaded successfully"
- ✅ Dialog closes
- ✅ Document appears in Documents section
- ✅ Document appears in left nav tree under "External Resources"
- ✅ Document has correct PDF icon

**Pass/Fail:** ___________

---

### 2. Upload Multiple File Types

**Test Steps:**
1. Upload each file type:
   - PDF (.pdf)
   - Word (.docx)
   - Excel (.xlsx)
   - PowerPoint (.pptx)
   - Text (.txt)
   - Markdown (.md)
   - Image (.png, .jpg, .gif, .webp)

**Expected Results:**
- ✅ All file types upload successfully
- ✅ Each file has appropriate icon:
  - PDF: File with "PDF" icon
  - Word: Document icon
  - Excel: Table/spreadsheet icon
  - PowerPoint: Slideshow icon
  - Text/Markdown: Text snippet icon
  - Images: Image icon

**Pass/Fail:** ___________

---

### 3. File Validation - Size Limit

**Test Steps:**
1. Attempt to upload file > 200MB

**Expected Results:**
- ✅ Error message: "File size exceeds maximum allowed size of 200 MB"
- ✅ Upload is prevented
- ✅ No API call is made

**Pass/Fail:** ___________

---

### 4. File Validation - File Type

**Test Steps:**
1. Attempt to upload .exe file or other invalid type

**Expected Results:**
- ✅ Error message: "File type not allowed"
- ✅ Upload is prevented
- ✅ File picker should filter to allowed types

**Pass/Fail:** ___________

---

### 5. Duplicate File Names (Auto-Rename)

**Test Steps:**
1. Upload a file named "test.pdf"
2. Upload another file with same name "test.pdf"
3. Upload a third file with same name "test.pdf"

**Expected Results:**
- ✅ First file: Title = "test"
- ✅ Second file: Title = "test (2)"
- ✅ Third file: Title = "test (3)"
- ✅ All files appear in list
- ✅ No errors or conflicts

**Pass/Fail:** ___________

---

### 6. Download Document (Tree Navigation)

**Test Steps:**
1. Navigate to any page in the app
2. Expand "External Resources" in left nav tree
3. Click on a document

**Expected Results:**
- ✅ Download starts automatically
- ✅ Browser handles download (new tab or download prompt)
- ✅ Success message: "Downloading [filename]"
- ✅ File downloads correctly and can be opened

**Pass/Fail:** ___________

---

### 7. Download Document (WorldDetail Page)

**Test Steps:**
1. Navigate to World Detail page
2. Scroll to Documents section
3. Click download icon on a document

**Expected Results:**
- ✅ Download starts automatically
- ✅ Success message: "Downloading [filename]"
- ✅ File downloads correctly

**Pass/Fail:** ___________

---

### 8. Edit Document Title (GM)

**Test Steps:**
1. As GM, navigate to World Detail page
2. In Documents section, click Edit icon on a document
3. Change title to "Updated Title"
4. Click checkmark to save

**Expected Results:**
- ✅ Inline edit form appears
- ✅ Title field is populated with current title
- ✅ Save button becomes active
- ✅ Document title updates in list
- ✅ Document title updates in tree navigation
- ✅ Success message: "Document updated"
- ✅ Edit mode closes

**Pass/Fail:** ___________

---

### 9. Edit Document Description (GM)

**Test Steps:**
1. As GM, click Edit on a document
2. Add/modify description: "This is a test description"
3. Click checkmark to save

**Expected Results:**
- ✅ Description field is editable
- ✅ Description updates and displays below title
- ✅ Success message: "Document updated"

**Pass/Fail:** ___________

---

### 10. Cancel Document Edit

**Test Steps:**
1. Click Edit on a document
2. Modify title/description
3. Click X (cancel) button

**Expected Results:**
- ✅ Changes are discarded
- ✅ Document reverts to original title/description
- ✅ Edit mode closes
- ✅ No API call is made

**Pass/Fail:** ___________

---

### 11. Delete Document (GM)

**Test Steps:**
1. As GM, click Delete icon on a document
2. Click "Delete" in confirmation dialog

**Expected Results:**
- ✅ Confirmation dialog appears: "Are you sure you want to delete..."
- ✅ Document is removed from Documents list
- ✅ Document is removed from tree navigation
- ✅ Success message: "Document deleted"
- ✅ File is deleted from blob storage

**Pass/Fail:** ___________

---

### 12. Cancel Delete Document

**Test Steps:**
1. Click Delete icon on a document
2. Click "Cancel" in confirmation dialog

**Expected Results:**
- ✅ Dialog closes
- ✅ Document remains in list
- ✅ No changes made

**Pass/Fail:** ___________

---

### 13. Permissions - Non-GM Upload

**Test Steps:**
1. Log in as non-GM user (regular member)
2. Navigate to World Detail page
3. Check Documents section

**Expected Results:**
- ✅ "Upload Document" button is NOT visible
- ✅ Edit icons are NOT visible on documents
- ✅ Delete icons are NOT visible on documents
- ✅ Download icon IS visible
- ✅ Documents can be downloaded

**Pass/Fail:** ___________

---

### 14. Permissions - Non-GM API Attempt

**Test Steps:**
1. As non-GM user, attempt to call upload API directly (e.g., via browser console)

**Expected Results:**
- ✅ API returns 403 Forbidden
- ✅ Upload is prevented
- ✅ Error message indicates permission denied

**Pass/Fail:** ___________

---

### 15. Empty State

**Test Steps:**
1. Create new world with no documents
2. Navigate to World Detail page

**Expected Results:**
- ✅ Documents section shows message: "No documents uploaded yet. Upload PDFs, Office files, images, or other documents."
- ✅ Upload button is visible (for GM)
- ✅ External Resources in tree shows only links (no documents)

**Pass/Fail:** ___________

---

### 16. File Size Display

**Test Steps:**
1. Upload files of various sizes:
   - < 1 KB
   - ~ 500 KB
   - ~ 5 MB
   - ~ 50 MB

**Expected Results:**
- ✅ File sizes display with appropriate units:
  - Small: "512 B"
  - Medium: "487.3 KB"
  - Large: "4.88 MB"
- ✅ Formatting is human-readable with 2 decimal places

**Pass/Fail:** ___________

---

### 17. Upload Date Display

**Test Steps:**
1. Upload a document
2. Check upload date display

**Expected Results:**
- ✅ Upload date shows in format: "MMM d, yyyy" (e.g., "Jan 2, 2026")
- ✅ Date is accurate

**Pass/Fail:** ___________

---

### 18. Tree Navigation Icons

**Test Steps:**
1. Upload one of each supported file type
2. Check icons in tree navigation under External Resources

**Expected Results:**
- ✅ Each document has correct icon matching file type
- ✅ Icons are distinct from external link icons
- ✅ Icons are clearly visible

**Pass/Fail:** ___________

---

### 19. Concurrent Editing (Edge Case)

**Test Steps:**
1. Open same world in two browser tabs as GM
2. In Tab 1: Start editing document title
3. In Tab 2: Delete the same document
4. In Tab 1: Try to save

**Expected Results:**
- ✅ Tab 1 shows error: "Document not found" or similar
- ✅ No data corruption
- ✅ Refresh shows correct state

**Pass/Fail:** ___________

---

### 20. Network Error Handling

**Test Steps:**
1. Disconnect network
2. Try to upload document
3. Try to download document
4. Try to delete document

**Expected Results:**
- ✅ Clear error messages for each operation
- ✅ No silent failures
- ✅ UI remains responsive

**Pass/Fail:** ___________

---

### 21. Upload Progress Indicator

**Test Steps:**
1. Upload large file (close to 200MB)
2. Observe progress indicator

**Expected Results:**
- ✅ Progress bar is visible
- ✅ Status text updates ("Requesting upload URL...", "Uploading file...", "Finalizing...")
- ✅ Dialog cannot be closed while uploading
- ✅ Upload completes successfully

**Pass/Fail:** ___________

---

### 22. Special Characters in Filename

**Test Steps:**
1. Upload file with special characters: "Test & Document (2024) #1.pdf"

**Expected Results:**
- ✅ File uploads successfully
- ✅ Filename is sanitized for blob path
- ✅ Display title preserves original characters
- ✅ Download works correctly

**Pass/Fail:** ___________

---

## Integration Tests

### 23. Tree State Refresh After Upload

**Test Steps:**
1. Upload document
2. Verify tree navigation updates immediately

**Expected Results:**
- ✅ New document appears in tree without manual refresh
- ✅ Document count is accurate

**Pass/Fail:** ___________

---

### 24. Tree State Refresh After Delete

**Test Steps:**
1. Delete document from WorldDetail page
2. Check tree navigation

**Expected Results:**
- ✅ Document is removed from tree immediately
- ✅ No broken links or errors

**Pass/Fail:** ___________

---

### 25. Multiple Worlds Isolation

**Test Steps:**
1. Upload document to World A
2. Navigate to World B
3. Check documents section

**Expected Results:**
- ✅ World B does not show World A's documents
- ✅ Documents are properly isolated by WorldId

**Pass/Fail:** ___________

---

## Performance Tests

### 26. Large File Upload Performance

**Test Steps:**
1. Upload 100MB file
2. Measure time to complete

**Expected Results:**
- ✅ Upload completes in reasonable time (< 2 minutes on decent connection)
- ✅ No timeout errors
- ✅ UI remains responsive

**Pass/Fail:** ___________

---

### 27. Many Documents Display

**Test Steps:**
1. Upload 50+ documents to single world
2. Navigate to WorldDetail page

**Expected Results:**
- ✅ Page loads in < 3 seconds
- ✅ All documents render correctly
- ✅ Scrolling is smooth
- ✅ Tree navigation handles many items

**Pass/Fail:** ___________

---

## Security Tests

### 28. SAS Token Expiration

**Test Steps:**
1. Generate download URL for document
2. Wait 20 minutes (SAS tokens expire at 15 minutes)
3. Try to use expired URL

**Expected Results:**
- ✅ Expired token is rejected by Azure
- ✅ User can generate new download URL
- ✅ No security vulnerability

**Pass/Fail:** ___________

---

### 29. Cross-World Document Access

**Test Steps:**
1. As non-member, try to access document from another world (via API)

**Expected Results:**
- ✅ 403 Forbidden response
- ✅ Download is prevented
- ✅ No data leakage

**Pass/Fail:** ___________

---

## Browser Compatibility

### 30. Chrome
- ✅ All features work
- ✅ File upload works
- ✅ Download works

**Pass/Fail:** ___________

### 31. Firefox
- ✅ All features work
- ✅ File upload works
- ✅ Download works

**Pass/Fail:** ___________

### 32. Edge
- ✅ All features work
- ✅ File upload works
- ✅ Download works

**Pass/Fail:** ___________

### 33. Safari
- ✅ All features work
- ✅ File upload works
- ✅ Download works

**Pass/Fail:** ___________

---

## Test Summary

**Total Tests:** 33
**Passed:** ___________
**Failed:** ___________
**Blocked:** ___________

**Critical Issues Found:**


**Non-Critical Issues Found:**


**Tested By:** ___________
**Date:** ___________
**Build Version:** ___________

---

## Notes

