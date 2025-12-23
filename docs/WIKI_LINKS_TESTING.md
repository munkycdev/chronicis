# Wiki Links Feature - Testing Checklist

**Version:** 1.0  
**Date:** December 23, 2025  
**Purpose:** Comprehensive testing guide for wiki links feature

---

## Pre-Test Setup

- [ ] Backend API running (Azure Functions)
- [ ] Frontend running (Blazor WASM)
- [ ] Logged in with valid user account
- [ ] At least 2-3 test articles created in a World
- [ ] Browser console open (F12) to watch for errors

---

## Phase 1: Basic Link Creation & Autocomplete

### Test 1.1: Trigger Autocomplete
- [ ] Open an article in the editor
- [ ] Type `[[` in the editor
- [ ] **Expected:** Nothing happens yet (need 3+ characters)
- [ ] Type `[[ab` (only 2 chars after `[[`)
- [ ] **Expected:** Still no autocomplete
- [ ] Type `[[abc` (3 chars)
- [ ] **Expected:** Autocomplete dropdown appears below cursor
- [ ] **Check console:** "✅ Wiki link autocomplete initialized for" message

### Test 1.2: Autocomplete Results
- [ ] With autocomplete open, verify suggestions appear
- [ ] **Expected:** List of articles matching "abc"
- [ ] **Expected:** Each shows Title and DisplayPath
- [ ] **Expected:** Loading spinner if API is slow
- [ ] **Expected:** "No articles found" if no matches

### Test 1.3: Keyboard Navigation
- [ ] With autocomplete open, press **Arrow Down**
- [ ] **Expected:** Selection moves to next item (highlight changes)
- [ ] Press **Arrow Up**
- [ ] **Expected:** Selection moves to previous item
- [ ] Press **Arrow Down** multiple times to cycle through all items
- [ ] **Expected:** Wraps to first item after last
- [ ] Press **Escape**
- [ ] **Expected:** Autocomplete closes, `[[abc` remains in text

### Test 1.4: Select with Enter
- [ ] Type `[[test` to trigger autocomplete (where "test" matches an article)
- [ ] Use arrows to select an article
- [ ] Press **Enter**
- [ ] **Expected:** `[[test` is replaced with a wiki link
- [ ] **Expected:** Link displays article title
- [ ] **Expected:** Link styled with beige-gold color
- [ ] **Expected:** Autocomplete closes
- [ ] **Check console:** "✅ Wiki link inserted:" message

### Test 1.5: Select with Click
- [ ] Type `[[another` to trigger autocomplete
- [ ] Click on a suggestion with mouse
- [ ] **Expected:** Same behavior as Enter key
- [ ] **Expected:** Link inserted successfully

### Test 1.6: Cancel Autocomplete
- [ ] Type `[[cancel`
- [ ] Press **Escape**
- [ ] **Expected:** Autocomplete closes
- [ ] **Expected:** Text `[[cancel` remains unchanged

---

## Phase 2: Link Display & Styling

### Test 2.1: Valid Link Appearance
- [ ] Create a wiki link to an existing article
- [ ] **Expected:** Link shows beige-gold color (#C4AF8E)
- [ ] **Expected:** Subtle underline visible
- [ ] Hover over the link
- [ ] **Expected:** Cursor changes to pointer
- [ ] **Expected:** Slight glow/highlight appears

### Test 2.2: Link Saves to Markdown
- [ ] Create a wiki link in editor
- [ ] Save the article (wait for "✓ Saved" message)
- [ ] Refresh the page (F5)
- [ ] **Expected:** Link still appears correctly
- [ ] **Expected:** Link is clickable

---

## Phase 3: Link Navigation

### Test 3.1: Click Valid Link
- [ ] Click on a valid wiki link
- [ ] **Expected:** Navigates to the target article
- [ ] **Expected:** URL changes to `/article/{slug}`
- [ ] **Expected:** Target article loads in editor
- [ ] **Check console:** "Wiki link clicked:" message

### Test 3.2: Navigation from Article Tree
- [ ] Navigate to different article using tree
- [ ] Go back to article with links
- [ ] Click link again
- [ ] **Expected:** Navigation still works

---

## Phase 4: Broken Link Detection

### Test 4.1: Create Broken Link Manually
- [ ] In database or via API, note an article's GUID
- [ ] Create a link using that GUID: `[[a1b2c3d4-e5f6-7890-abcd-ef1234567890]]`
- [ ] Delete the target article
- [ ] Reload the article with the link
- [ ] **Expected:** Link appears red/error colored
- [ ] **Expected:** Link has strikethrough or dashed underline
- [ ] **Check console:** "Found X broken links" warning

### Test 4.2: Click Broken Link
- [ ] Click on the broken link
- [ ] **Expected:** Broken Link Dialog appears
- [ ] **Expected:** Dialog shows three options:
  - Remove Link
  - Convert to Text
  - Cancel

---

## Phase 5: Broken Link Recovery

### Test 5.1: Remove Broken Link
- [ ] Click broken link to open dialog
- [ ] Click **Remove Link** button
- [ ] **Expected:** Link completely disappears from text
- [ ] **Expected:** Dialog closes
- [ ] **Expected:** Toast: "Link removed"

### Test 5.2: Convert Broken Link to Text
- [ ] Create another broken link
- [ ] Click it to open dialog
- [ ] Click **Convert to Text** button
- [ ] **Expected:** Link becomes plain text (no styling)
- [ ] **Expected:** Text content preserved
- [ ] **Expected:** Dialog closes
- [ ] **Expected:** Toast: "Link converted to text"

### Test 5.3: Cancel Recovery
- [ ] Click broken link to open dialog
- [ ] Click **Cancel** button
- [ ] **Expected:** Dialog closes
- [ ] **Expected:** Link unchanged (still broken)

---

## Phase 6: Autocomplete Edge Cases

### Test 6.1: No Matches
- [ ] Type `[[xyznonexistent`
- [ ] **Expected:** Autocomplete shows "No articles found"
- [ ] **Expected:** No error in console

### Test 6.2: Many Matches
- [ ] Type `[[a` (single character that matches many articles)
- [ ] **Expected:** Autocomplete shows top 10 results only
- [ ] **Expected:** Results sorted alphabetically

### Test 6.3: Special Characters
- [ ] Type `[[test's` (with apostrophe)
- [ ] **Expected:** Autocomplete works correctly
- [ ] **Expected:** No errors

### Test 6.4: Typing Continues
- [ ] Type `[[test`
- [ ] Autocomplete appears
- [ ] Continue typing: `ing`
- [ ] **Expected:** Autocomplete updates with new query "testing"
- [ ] **Expected:** Results filter down

---

## Phase 7: Multiple Links

### Test 7.1: Multiple Links in Same Article
- [ ] Create 3-4 different wiki links in one article
- [ ] Save the article
- [ ] Reload the page
- [ ] **Expected:** All links appear correctly
- [ ] Click each link
- [ ] **Expected:** Each navigates to correct article

### Test 7.2: Mixed Valid and Broken Links
- [ ] Create 2 valid links and 1 broken link
- [ ] **Expected:** Valid links show in beige-gold
- [ ] **Expected:** Broken link shows in red
- [ ] **Expected:** Can click and navigate valid links
- [ ] **Expected:** Can recover broken link

---

## Phase 8: Markdown Round-Trip

### Test 8.1: Save and Reload
- [ ] Type some text: "Check out "
- [ ] Create a wiki link
- [ ] Type more text: " for more info"
- [ ] Save article
- [ ] Note the raw markdown in database (should be `[[guid]]` or `[[guid|display]]`)
- [ ] Reload page
- [ ] **Expected:** Link renders correctly
- [ ] **Expected:** Text before and after preserved

### Test 8.2: Edit Existing Link
- [ ] Open article with existing wiki link
- [ ] Place cursor after the link
- [ ] Type more text
- [ ] **Expected:** Can type normally
- [ ] Delete the link text
- [ ] **Expected:** Link removed
- [ ] **Expected:** No errors

---

## Phase 9: Console Checks

### Test 9.1: JavaScript Loading
- [ ] Open browser console before loading page
- [ ] Load the application
- [ ] **Check for these messages:**
  - [ ] "✅ Wiki link extension loaded"
  - [ ] "✅ Wiki link autocomplete script loaded"
  - [ ] "✅ Wiki link broken link detection script loaded"
  - [ ] "✅ TipTap editor created with ID: tiptap-editor-{guid}"
  - [ ] "✅ Wiki link autocomplete initialized for tiptap-editor-{guid}"

### Test 9.2: No Errors
- [ ] Perform all tests above
- [ ] **Expected:** NO red errors in console
- [ ] **Expected:** Only warnings (yellow) are acceptable for logging

---

## Phase 10: Performance

### Test 10.1: Autocomplete Speed
- [ ] Type `[[test`
- [ ] Note time for autocomplete to appear
- [ ] **Expected:** < 500ms
- [ ] Type one more character
- [ ] **Expected:** Results update quickly (< 300ms)

### Test 10.2: Broken Link Detection
- [ ] Open article with 5+ links
- [ ] Note load time
- [ ] **Expected:** Article loads normally
- [ ] **Check console:** Should log resolution time in ms
- [ ] **Expected:** < 1 second for resolution

---

## Common Issues & Fixes

### Issue: Autocomplete doesn't appear
**Check:**
- [ ] Console for JavaScript errors
- [ ] Article has been selected/loaded
- [ ] Typed at least 3 characters after `[[`
- [ ] WorldId is set (user is in a world context)

### Issue: Links don't navigate
**Check:**
- [ ] Console for "Wiki link clicked:" message
- [ ] Target article exists
- [ ] Not a broken link (should be beige-gold, not red)

### Issue: Broken links not detected
**Check:**
- [ ] Console for "Found X broken links" message
- [ ] Link resolution API is working (check Network tab)
- [ ] Target article actually deleted

### Issue: Disposed object error
**Check:**
- [ ] Only happens on initial load?
- [ ] Try navigating to different article and back
- [ ] Check component lifecycle (should create DotNetHelper in OnInitialized)

---

## Success Criteria Summary

**ALL tests passing means:**
- ✅ Autocomplete triggers correctly with `[[`
- ✅ Keyboard navigation works (arrows, Enter, Escape)
- ✅ Links insert and save properly
- ✅ Links navigate to correct articles
- ✅ Broken links detected and styled
- ✅ Broken link recovery dialog works
- ✅ No console errors
- ✅ Good performance (< 1s for most operations)

---

## Quick Smoke Test (5 minutes)

If you want a quick validation:

1. [ ] Open an article
2. [ ] Type `[[test` and verify autocomplete appears
3. [ ] Select an item with Enter
4. [ ] Verify link inserted with beige-gold color
5. [ ] Click the link and verify navigation
6. [ ] Check console for no red errors

If those work, the feature is functional!

---

*End of Testing Checklist*
