# Implementation Plan: Handwritten Session Notes

## Overview

Implement handwritten note capture for session notes using an HTML5 Canvas component, server-side PNG storage via WorldDocument infrastructure, AI/OCR transcription, and a tab-based UI separating handwritten and transcribed views. The implementation uses C# for API/services, Blazor for client components, and JavaScript interop for the canvas.

## Tasks

- [x] 1. Data model and storage infrastructure
  - [x] 1.1 Add `HandwrittenNoteImageId` to Article entity and create EF migration
    - Add nullable `Guid? HandwrittenNoteImageId` property and `WorldDocument? HandwrittenNoteImage` navigation property to the `Article` entity
    - Create EF Core migration adding the column with FK constraint and filtered index
    - Update `ArticleDto` to include `HandwrittenNoteImageId`
    - _Requirements: 8.2_

  - [x] 1.2 Create DTOs for handwritten note operations
    - Create `HandwrittenNoteSaveResultDto` with `DocumentId` and `DownloadUrl`
    - Create `HandwrittenNoteTranscribeResultDto` with `DocumentId`, `DownloadUrl`, and `TranscribedText`
    - Create `TranscriptionResultDto` with `Success`, `Text`, and `ErrorMessage`
    - _Requirements: 3.3, 4.3_

  - [x] 1.3 Write property test for View State Determination (Property 1)
    - **Property 1: View State Determination**
    - Generate random `Guid?` values for `HandwrittenNoteImageId`; verify that non-null → Tab_UI displayed, null → "Add a handwritten note" button displayed, mutually exclusive
    - **Validates: Requirements 1.1, 1.3, 5.1**

- [x] 2. Transcription service
  - [x] 2.1 Create `ITranscriptionService` interface and implementation
    - Define `ITranscriptionService` with `TranscribeImageAsync(byte[] imageBytes, CancellationToken)` method
    - Implement `TranscriptionService` that calls the external AI/OCR API
    - Use `CancellationTokenSource` with 60-second timeout
    - Handle empty results, failures, and timeout scenarios
    - _Requirements: 4.3, 4.4, 4.5_

  - [x] 2.2 Write unit tests for TranscriptionService
    - Mock HTTP client for external API calls
    - Test success, empty result, failure, and timeout scenarios
    - _Requirements: 4.3, 4.4, 4.5_

- [x] 3. Handwritten note API service
  - [x] 3.1 Create `IHandwrittenNoteService` interface and implementation
    - Define `IHandwrittenNoteService` with `SaveAsync`, `TranscribeAsync`, `GetImageDownloadUrlAsync`, `DeleteAsync`
    - Implement `HandwrittenNoteService` orchestrating `IWorldDocumentService`, `IBlobStorageService`, `ITranscriptionService`, and `DbContext`
    - Save: create WorldDocument with `ContentType = "image/png"`, set article's `HandwrittenNoteImageId`
    - Replace: delete old WorldDocument (record + blob) before creating new one
    - Transcribe: save image, then call TranscriptionService, store result in Article.Body
    - Delete: remove WorldDocument record and blob, null out `HandwrittenNoteImageId`
    - Handle blob deletion failure during cascading delete (log warning, proceed)
    - _Requirements: 3.3, 3.4, 4.1, 4.2, 4.3, 4.4, 4.5, 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_

  - [x] 3.2 Write property test for Save Persists and Links (Property 4)
    - **Property 4: Save Persists and Links Handwritten Note**
    - Generate random valid PNG byte stubs and article states; verify WorldDocument created with `ContentType = "image/png"` and article `HandwrittenNoteImageId` references new document
    - **Validates: Requirements 3.3, 8.2**

  - [x] 3.3 Write property test for Transcription Stores Result (Property 5)
    - **Property 5: Transcription Stores Result in Body**
    - Generate random non-empty strings; verify transcription result stored in Article.Body field
    - **Validates: Requirements 4.3**

  - [x] 3.4 Write property test for Cascade Delete (Property 7)
    - **Property 7: Article Deletion Cascades to Handwritten Note Cleanup**
    - Generate articles with/without `HandwrittenNoteImageId`; verify delete removes WorldDocument and blob when present
    - **Validates: Requirements 8.3**

  - [x] 3.5 Write property test for Replace Overwrites (Property 8)
    - **Property 8: Replace Overwrites Old Handwritten Note**
    - Generate articles with existing handwritten note, save new; verify old deleted and new linked
    - **Validates: Requirements 8.6**

  - [x] 3.6 Write property test for Summary Enablement (Property 6)
    - **Property 6: Summary Enablement Based on Body Content**
    - Generate random nullable strings (null, empty, whitespace, content); verify AI summary enabled iff Body is non-null with at least one non-whitespace char
    - **Validates: Requirements 7.1, 7.3**

  - [x] 3.7 Write unit tests for HandwrittenNoteService
    - Mock all dependencies (`IWorldDocumentService`, `IBlobStorageService`, `ITranscriptionService`, `DbContext`)
    - Test save, replace, transcribe, get URL, delete, error scenarios
    - _Requirements: 3.3, 3.4, 4.1, 4.2, 4.3, 4.4, 4.5, 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_

- [x] 4. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. API controller
  - [x] 5.1 Create `HandwrittenNoteController`
    - Base route: `api/articles/{articleId}/handwritten-note`
    - POST `/` — upload/replace handwritten note PNG (accept byte array, validate non-empty, enforce auth)
    - POST `/transcribe` — save + transcribe (accept byte array, enforce auth, handle overwrite confirmation via request param)
    - GET `/` — get download URL for handwritten note image
    - DELETE `/` — delete handwritten note image
    - Enforce authentication using Auth0 bearer tokens
    - Return appropriate HTTP status codes for all error scenarios
    - _Requirements: 3.2, 3.3, 3.4, 4.1, 4.2, 4.3, 4.4, 4.5, 8.5, 8.6_

  - [x] 5.2 Write unit tests for HandwrittenNoteController
    - Mock `IHandwrittenNoteService`
    - Verify HTTP responses (200, 400, 404, 500) and auth enforcement
    - Test all endpoints and error paths
    - _Requirements: 3.2, 3.3, 3.4, 4.1, 4.2, 8.5, 8.6_

- [x] 6. Client API service
  - [x] 6.1 Create `IHandwrittenNoteApiService` interface and implementation
    - Define interface with `SaveHandwrittenNoteAsync`, `TranscribeHandwrittenNoteAsync`, `GetHandwrittenNoteUrlAsync`, `DeleteHandwrittenNoteAsync`
    - Implement using `HttpClient` following existing `HttpClientExtensions` patterns
    - Map HTTP errors to user-friendly messages
    - _Requirements: 3.2, 3.3, 4.1, 8.5_

  - [x] 6.2 Write unit tests for HandwrittenNoteApiService
    - Mock `HttpClient`
    - Verify request serialization, response deserialization, error mapping
    - _Requirements: 3.2, 3.3, 4.1, 8.5_

- [x] 7. Drawing canvas component
  - [x] 7.1 Create `drawingCanvas.js` JavaScript interop module
    - Handle pointer events (pointerdown, pointermove, pointerup) for stylus/touch/mouse
    - Implement stroke storage as array of point arrays with pressure data
    - Implement pressure-to-width mapping: width = clamp(pressure * 8, 1, 8); default 2px when pressure absent
    - Render strokes within 16ms of input event
    - Implement undo/redo via stroke history stack
    - Implement pen tool with 6+ selectable colors (including black, red, blue) and stroke-level eraser
    - Export canvas content as PNG via `canvas.toBlob()`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [x] 7.2 Create `DrawingCanvas.razor` Blazor component
    - Parameters: `EventCallback<byte[]> OnSave`, `EventCallback<byte[]> OnTranscribe`, `bool IsSaving`
    - Full-width canvas with minimum 2000px height, vertical scrolling
    - Color picker toolbar with 6+ colors
    - Pen/Eraser toggle
    - Undo/Redo buttons
    - Save button (disabled when no strokes, disabled during save)
    - Transcribe button (disabled when no strokes, disabled during save)
    - JS interop calls for initialization, undo, redo, export
    - _Requirements: 2.1, 2.4, 2.6, 3.1, 3.2_

  - [x] 7.3 Write property test for Pressure-to-Width Mapping (Property 2)
    - **Property 2: Pressure-to-Width Mapping**
    - Generate random floats in [0.0, 1.0]; verify computed stroke width in [1, 8] and monotonically non-decreasing
    - **Validates: Requirements 2.2**

  - [x] 7.4 Write property test for Undo/Redo Round-Trip (Property 3)
    - **Property 3: Undo/Redo Round-Trip**
    - Generate random stroke sequences; verify undo followed by redo returns stroke list to same state
    - **Validates: Requirements 2.6**

  - [x] 7.5 Write unit tests for DrawingCanvas component logic
    - Test save button disabled state (no strokes, during save)
    - Test transcribe button disabled state
    - Test JS interop call invocations
    - _Requirements: 2.1, 3.1, 3.2_

- [x] 8. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 9. Tab UI and session note page integration
  - [x] 9.1 Create `HandwrittenNoteTabView.razor` component
    - Parameters: `string? ImageDownloadUrl`, `string? Body`, `bool ShowTranscribedTabActive`, `EventCallback OnTranscribeRequested`, `EventCallback<string> OnBodyChanged`
    - MudTabs with "Handwritten" and "Transcribed" tabs
    - Handwritten tab: display saved image at full width with scroll/pinch-to-zoom
    - Transcribed tab: render TipTap editor with existing extensions (WikiLinks, ExternalReferences, formatting)
    - Empty transcribed state: show message + "Transcribe" button
    - Tab switch within 200ms, no full page reload
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

  - [x] 9.2 Integrate handwritten note flow into Session_Note_Page
    - Add "Add a handwritten note" button when `HandwrittenNoteImageId` is null
    - Show Tab_UI when `HandwrittenNoteImageId` is non-null
    - Wire DrawingCanvas `OnSave` → call `IHandwrittenNoteApiService.SaveHandwrittenNoteAsync`
    - Wire DrawingCanvas `OnTranscribe` → call `IHandwrittenNoteApiService.TranscribeHandwrittenNoteAsync`
    - Handle overwrite confirmation when Body already has content before transcription
    - Post-save: navigate to Tab_UI
    - Post-transcribe: navigate to Tab_UI with Transcribed tab active
    - Error handling: display snackbar on failures, preserve canvas content, re-enable buttons
    - Display error and hide button when article state fails to load
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 3.4, 3.5, 4.1, 4.2, 4.6, 4.7_

  - [x] 9.3 Integrate rich editing features in transcribed content tab
    - Ensure TipTap editor supports WikiLink `[[]]` autocomplete
    - Ensure ExternalReference insertion works
    - Ensure formatting (H1–H3, bold, italic, underline, strikethrough, lists, blockquotes, code blocks) works
    - Wire auto-save for Body field changes
    - Display error notification on save failure, retain unsaved changes
    - Navigation guard for unsaved changes
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_

  - [x] 9.4 Integrate AI summary enablement logic
    - Enable AI summary feature when Body is non-null with ≥1 non-whitespace character
    - Disable with message when Body is null/empty/whitespace
    - Use existing summary workflow with Body as source material
    - Handle generation failure with error + retry
    - _Requirements: 7.1, 7.2, 7.3, 7.4_

  - [x] 9.5 Write unit tests for HandwrittenNoteTabView component
    - Test tab rendering, active tab logic, empty state display
    - Test transcribe button callback
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

  - [x] 9.6 Write unit tests for session note page integration
    - Mock `IHandwrittenNoteApiService`
    - Test button vs Tab_UI state transitions
    - Test save/transcribe flows with success and error scenarios
    - Test overwrite confirmation dialog
    - Test error display on load failure
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 3.4, 3.5, 4.1, 4.2, 4.6, 4.7_

- [x] 10. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document using FsCheck
- Unit tests validate specific examples, edge cases, and achieve 100% line/branch coverage per AGENTS.md
- All dependencies must be mocked in unit tests per AGENTS.md testing requirements
- JavaScript interop for canvas operations requires separate JS module testing approach
- The existing TipTap editor, WikiLink autocomplete, and ExternalReference insertion are reused without modification

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2"] },
    { "id": 1, "tasks": ["1.3", "2.1"] },
    { "id": 2, "tasks": ["2.2", "3.1"] },
    { "id": 3, "tasks": ["3.2", "3.3", "3.4", "3.5", "3.6", "3.7"] },
    { "id": 4, "tasks": ["5.1", "6.1", "7.1"] },
    { "id": 5, "tasks": ["5.2", "6.2", "7.2"] },
    { "id": 6, "tasks": ["7.3", "7.4", "7.5"] },
    { "id": 7, "tasks": ["9.1", "9.2"] },
    { "id": 8, "tasks": ["9.3", "9.4"] },
    { "id": 9, "tasks": ["9.5", "9.6"] }
  ]
}
```
