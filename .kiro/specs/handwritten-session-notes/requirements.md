# Requirements Document

## Introduction

This feature adds handwritten note capture to Chronicis session notes. Users can draw/write on a canvas (optimized for iPad stylus input), save the image, transcribe it to text, and then use the transcribed content with all existing session note features (WikiLinks, ExternalReferences, AI summaries, etc.). The handwritten and transcribed views are separated by a tab UI within the session note.

## Glossary

- **Drawing_Canvas**: The interactive drawing surface rendered in the browser that accepts stylus/touch/mouse input for freehand writing and drawing.
- **Handwritten_Note**: A raster image captured from the Drawing_Canvas, stored as a binary asset associated with a session note Article record.
- **Transcription**: The text output produced by an AI/OCR service from a Handwritten_Note image.
- **Session_Note_Page**: The existing Blazor page that displays a session note Article, including its body content, WikiLinks, ExternalReferences, and AI summary features.
- **Tab_UI**: A MudBlazor tab component (`MudTabs`) that separates the handwritten note view from the transcribed content view within the Session_Note_Page.
- **Transcription_Service**: A backend service that accepts an image and returns transcribed text content.
- **Session_Note_Article**: An Article entity with `ArticleType.SessionNote` and a non-null `SessionId`, representing a player or GM contribution to a session.

## Requirements

### Requirement 1: Add Handwritten Note Entry Point

**User Story:** As a GM or player, I want to click "Add a handwritten note" from the session note parent page, so that I can begin capturing handwritten input.

#### Acceptance Criteria

1. WHEN a user navigates to the Session_Note_Page AND no Handwritten_Note exists for the Session_Note_Article, THE Session_Note_Page SHALL display an "Add a handwritten note" button.
2. WHEN the user clicks the "Add a handwritten note" button, THE Session_Note_Page SHALL transition to the Drawing_Canvas view inline within the same page for that session note.
3. WHILE a Handwritten_Note already exists for the Session_Note_Article, THE Session_Note_Page SHALL display the Tab_UI instead of the "Add a handwritten note" button.
4. IF the system fails to determine whether a Handwritten_Note exists for the Session_Note_Article, THEN THE Session_Note_Page SHALL display an error message and hide the "Add a handwritten note" button until the page is refreshed.

### Requirement 2: Drawing Canvas for Handwritten Input

**User Story:** As a user with a stylus-capable device, I want a full-page drawing canvas, so that I can write session notes by hand.

#### Acceptance Criteria

1. WHEN the Drawing_Canvas view loads, THE Drawing_Canvas SHALL render a full-width writing surface with a minimum height of 2000 CSS pixels that scrolls vertically and accepts stylus, touch, and mouse input.
2. WHILE a stylus with pressure input is active, THE Drawing_Canvas SHALL render strokes with width varying between 1 and 8 CSS pixels proportional to the reported pressure value.
3. IF the input device does not report pressure data, THEN THE Drawing_Canvas SHALL render strokes at a fixed default width of 2 CSS pixels.
4. THE Drawing_Canvas SHALL provide drawing tools consisting of a pen with at least 6 selectable colors (including black, red, and blue) and a stroke-level eraser that removes entire strokes on contact.
5. WHEN the user draws on the Drawing_Canvas, THE Drawing_Canvas SHALL render each stroke segment to the screen within 16 milliseconds of receiving the input event.
6. THE Drawing_Canvas SHALL support undo and redo actions, each reversing or reapplying the last stroke operation.

### Requirement 3: Save Handwritten Note

**User Story:** As a user, I want to save my handwritten note as an image, so that it is persisted and associated with my session note.

#### Acceptance Criteria

1. IF the Drawing_Canvas contains no strokes, THEN THE "Save" button SHALL be disabled.
2. WHEN the user clicks the "Save" button on the Drawing_Canvas view, THE system SHALL disable the "Save" button to prevent duplicate submissions and export the Drawing_Canvas content as a PNG image.
3. WHEN the Drawing_Canvas content is exported, THE API SHALL store the Handwritten_Note image in the configured file storage and associate the storage reference with the Session_Note_Article record.
4. IF the save operation fails, THEN THE system SHALL display an error message describing the failure reason to the user, re-enable the "Save" button, and retain the Drawing_Canvas content so the user can retry.
5. WHEN the save completes, THE Session_Note_Page SHALL navigate back to the session note view displaying the saved Handwritten_Note in the Tab_UI.

### Requirement 4: Transcribe Handwritten Note

**User Story:** As a user, I want to transcribe my handwritten note to text, so that the content becomes searchable and usable with other Chronicis features.

#### Acceptance Criteria

1. WHEN the user clicks the "Transcribe" button on the Drawing_Canvas view, THE system SHALL first save the Handwritten_Note image (per Requirement 3), display a progress indicator, and then submit the image to the Transcription_Service.
2. IF the save operation fails during the Transcribe flow, THEN THE system SHALL display an error message indicating the save failed, retain the Drawing_Canvas content, and not submit the image to the Transcription_Service.
3. WHEN the Transcription_Service returns a non-empty result, THE API SHALL store the transcribed text in the Session_Note_Article Body field.
4. IF the Transcription_Service returns an empty result, THEN THE system SHALL display an error message indicating the transcription produced no text, while the Handwritten_Note image remains saved.
5. IF the Transcription_Service fails or does not respond within 60 seconds, THEN THE system SHALL display an error message indicating transcription failed, while the Handwritten_Note image remains saved.
6. WHEN transcription completes, THE Session_Note_Page SHALL navigate to the Tab_UI with the transcribed content tab active.
7. IF the Session_Note_Article Body field already contains content when the user initiates transcription, THEN THE system SHALL prompt the user to confirm before overwriting the existing content with the new transcription.

### Requirement 5: Tab UI for Handwritten and Transcribed Views

**User Story:** As a user, I want to switch between viewing my handwritten note image and the transcribed text, so that I can reference the original while working with the transcription.

#### Acceptance Criteria

1. WHILE a Handwritten_Note exists for the Session_Note_Article, THE Tab_UI SHALL display two tabs labeled "Handwritten" and "Transcribed", with the "Handwritten" tab active by default unless the user is navigated to the Tab_UI after a transcription completes.
2. WHEN the "Handwritten" tab is selected, THE Tab_UI SHALL display the saved Handwritten_Note image scaled to the full width of the tab content container, with browser-native scroll and pinch-to-zoom support for viewing detail.
3. WHEN the "Transcribed" tab is selected, THE Tab_UI SHALL display the transcribed content in the standard TipTap rich text editor.
4. WHILE no transcription exists for the Session_Note_Article, THE "Transcribed" tab SHALL display a message indicating no transcription is available and a "Transcribe" action button that initiates the transcription workflow defined in Requirement 4.
5. WHEN the user selects a tab, THE Tab_UI SHALL switch the displayed content within 200 milliseconds without a full page reload.

### Requirement 6: Rich Editing of Transcribed Content

**User Story:** As a user, I want the transcribed content to support all existing session note editing features, so that I can enrich the transcription with links, references, and formatting.

#### Acceptance Criteria

1. THE transcribed content editor SHALL support WikiLink insertion using `[[Article Title]]` syntax with autocomplete.
2. THE transcribed content editor SHALL support ExternalReference insertion from configured resource providers.
3. THE transcribed content editor SHALL support formatting including headings (H1–H3), bold, italic, underline, strikethrough, ordered lists, unordered lists, blockquotes, and code blocks.
4. WHEN the user edits the transcribed content, THE system SHALL save changes to the Session_Note_Article Body field using the same auto-save mechanism as typed session notes.
5. IF a save operation fails while the user is editing transcribed content, THEN THE system SHALL display an error notification and retain the unsaved changes in the editor so the user can retry.
6. WHEN the user attempts to navigate away from the transcribed content editor with unsaved changes, THE system SHALL display a confirmation prompt warning that unsaved changes will be lost.

### Requirement 7: AI Summary of Transcribed Notes

**User Story:** As a user, I want to generate an AI summary from my transcribed handwritten notes, so that I get the same summarization capabilities as typed session notes.

#### Acceptance Criteria

1. WHILE the Session_Note_Article Body field is non-null and contains at least 1 non-whitespace character, THE Session_Note_Page SHALL enable the AI summary generation feature using the existing summary workflow.
2. WHEN the user requests AI summary generation, THE system SHALL use the transcribed Body content as the source material, following the same process as typed session notes.
3. WHILE the Session_Note_Article Body field is null, empty, or contains only whitespace, THE AI summary generation feature SHALL be disabled with a message indicating transcription is required first.
4. IF AI summary generation fails, THEN THE system SHALL display an error message describing the failure and allow the user to retry.

### Requirement 8: Handwritten Note Storage

**User Story:** As a system operator, I want handwritten note images stored efficiently, so that storage costs remain manageable and images load quickly.

#### Acceptance Criteria

1. THE API SHALL store Handwritten_Note images as PNG files with content type `image/png` using the existing WorldDocument storage infrastructure, subject to the existing maximum file size limit.
2. THE API SHALL associate the stored image with the Session_Note_Article via a new nullable `HandwrittenNoteImageId` field referencing the WorldDocument record.
3. WHEN a Session_Note_Article is deleted, THE system SHALL delete the associated Handwritten_Note image from blob storage and remove the WorldDocument record from the database.
4. IF blob storage deletion fails during Session_Note_Article deletion, THEN THE system SHALL log the failure and proceed with removing the database records.
5. WHEN a client requests the Handwritten_Note image for a Session_Note_Article, THE API SHALL return a time-limited download URL consistent with existing WorldDocument download behavior so the client can render the image directly.
6. WHEN a user saves a new Handwritten_Note for a Session_Note_Article that already has an existing Handwritten_Note, THE API SHALL replace the previous image by deleting the old WorldDocument from storage and creating a new WorldDocument record.
