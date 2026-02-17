# ?? Chronicis Capture App - Implementation Plan v2.0

**Current Status:** v1.0 Complete
- ? Basic recording & transcription
- ? Speaker detection
- ? Audio + transcript export
- ? System tray integration
- ? Settings persistence

---

## ?? Feature List

| # | Feature | Priority | Complexity | Phase |
|---|---------|----------|------------|-------|
| 2 | Real-time Timestamp Markers | High | Low | 1 |
| 20 | Recording Timer Display | High | Low | 1 |
| 6 | Audio Level Monitoring | High | Medium | 2 |
| 24 | Crash Recovery | High | Medium | 2 |
| 9 | Speaker Naming During Recording | Medium | Medium | 3 |
| 3 | Background Noise Reduction | Medium | Medium | 3 |
| 26 | Transcription Quality Report | Medium | High | 4 |
| 13 | Character Name Dictionary | Low | High | 5 |
| 16 | AI Session Summary | Low | High | 5 |

---

## ?? Phase 1: Essential UX Improvements (Week 1)

**Goal:** Add features that improve the recording experience immediately

### Feature 2: Real-time Timestamp Markers

**What:** Button to add custom markers during recording that show up in transcript

**Implementation:**
```
Models/
  ??? TimestampMarker.cs (NEW)
  
Services/
  ??? IMarkerService.cs (NEW)
  ??? MarkerService.cs (NEW)
  
UI/
  ??? MainForm.cs (UPDATED)
      - Add "Add Marker" button
      - Quick input dialog for marker text
      - Show markers in transcript
```

**Technical Details:**
- Store markers with TimeSpan offset from start
- Inject markers into transcript at correct positions
- Export format: `[00:15:32] ?? Combat starts with dragon`
- Hotkey: F5 or configurable

**Success Criteria:**
- ? Can add marker during recording with button click
- ? Marker appears in transcript with timestamp
- ? Markers persist in saved transcript
- ? Markers saved in settings for session recovery

---

### Feature 20: Recording Timer Display

**What:** Prominent elapsed time display during recording

**Implementation:**
```
UI/
  ??? MainForm.cs (UPDATED)
      - Add timer label (large, visible)
      - Update every second
      - Show format: "Recording: 2:35:14"
      - Optional: Show estimated file size
```

**Technical Details:**
- Use System.Timers.Timer (not UI timer for accuracy)
- Calculate from recording start time
- Show in format: HH:MM:SS
- Optional: Show audio file size estimate (bytes/second * elapsed)

**Success Criteria:**
- ? Timer starts when recording starts
- ? Updates every second
- ? Visible and prominent in UI
- ? Stops when recording stops
- ? Shows in system tray tooltip on hover

**Time Estimate:** 3-4 days for Phase 1

---

## ?? Phase 2: Reliability & Monitoring (Week 2)

**Goal:** Prevent data loss and provide feedback during recording

### Feature 6: Audio Level Monitoring

**What:** Visual meter showing current audio input level

**Implementation:**
```
Services/
  ??? IAudioLevelMonitor.cs (NEW)
  ??? AudioLevelMonitor.cs (NEW)
  
UI/
  ??? MainForm.cs (UPDATED)
      - Add progress bar or custom control
      - Update in real-time (10-20 fps)
      - Color coding: green (good), yellow (low), red (clipping)
```

**Technical Details:**
- Calculate RMS (root mean square) from audio buffer
- Sample every 50-100ms for smooth updates
- Threshold detection:
  - Below -40dB: Red warning "Audio too quiet"
  - -40dB to -10dB: Green "Good"
  - Above -3dB: Red warning "Audio clipping"
- Add calibration: "Test your levels before recording"

**Success Criteria:**
- ? Visual meter responds to audio in real-time
- ? Shows before starting recording (preview mode)
- ? Color-coded feedback
- ? Warning if no audio detected for 5 seconds
- ? Low CPU usage (<1%)

---

### Feature 24: Crash Recovery

**What:** Auto-save progress and recover if app crashes

**Implementation:**
```
Services/
  ??? ICrashRecoveryService.cs (NEW)
  ??? CrashRecoveryService.cs (NEW)
  
Models/
  ??? RecoverySession.cs (NEW)
```

**Technical Details:**
- Auto-save every 30 seconds:
  - Current transcript (partial)
  - Audio file path
  - Recording metadata (start time, settings, speakers)
  - Marker list
- Save to: `%TEMP%/Chronicis/recovery_[guid].json`
- On startup:
  - Check for recovery files
  - Show dialog: "Recover unsaved session from [date]?"
  - Load and continue or discard
- Clean up recovery files after successful save

**Recovery File Format (JSON):**
```json
{
  "sessionId": "guid",
  "startTime": "2025-11-26T14:30:00Z",
  "audioFilePath": "C:/Temp/session_audio_123.wav",
  "transcript": "partial transcript...",
  "speakers": { "1": "DM", "2": "Alice" },
  "markers": [
    { "timestamp": "00:15:32", "text": "Combat starts" }
  ],
  "settings": { "model": "Base", "chunkSize": 5 }
}
```

**Success Criteria:**
- ? Recovery file created within 30s of recording start
- ? Updates every 30s during recording
- ? On crash, can recover partial session
- ? Recovered audio is playable
- ? Transcript includes everything up to crash
- ? Recovery files cleaned up after 7 days

**Time Estimate:** 5-6 days for Phase 2

---

## ?? Phase 3: Enhanced Speaker Management (Week 3)

**Goal:** Better speaker detection and management workflow

### Feature 9: Speaker Naming During Recording

**What:** Quick popup to name speakers as they're first detected

**Implementation:**
```
Services/
  ??? SpeakerDetectionService.cs (UPDATED)
      - Fire event on new speaker detected
      
UI/
  ??? SpeakerNameDialog.cs (NEW)
  ??? MainForm.cs (UPDATED)
      - Show non-blocking notification
      - Quick text input for speaker name
      - Option to "Name later"
```

**Technical Details:**
- When new speaker detected:
  - Show toast/notification: "New speaker detected (Speaker 3)"
  - Auto-dismiss after 5 seconds or user clicks
  - Text input with suggestions from character dictionary
- Non-modal dialog (doesn't interrupt recording)
- Can skip and name later via "Edit Speakers" button

**UI Mockup:**
```
???????????????????????????????????????
? ?? New Speaker Detected             ?
?                                     ?
? Speaker 3 just started talking.    ?
? Who is this?                        ?
?                                     ?
? [________________] [Save] [Later]  ?
?                                     ?
? Suggestions: Alice, Bob, Charlie    ?
???????????????????????????????????????
```

**Success Criteria:**
- ? Notification appears when new speaker detected
- ? Non-blocking (recording continues)
- ? Auto-dismiss after 5s if ignored
- ? Name updates in transcript immediately
- ? Shows suggestions from character dictionary

---

### Feature 3: Background Noise Reduction

**What:** Pre-process audio to reduce background noise before transcription

**Implementation:**
```
Services/
  ??? INoiseReductionService.cs (NEW)
  ??? NoiseReductionService.cs (NEW)
  
External:
  - Use RNNoise library (Xiph.org)
  - Or NAudio built-in filters
```

**Technical Details:**
- Apply noise gate filter:
  - Threshold: -40dB (configurable)
  - Attack/release times
- Apply high-pass filter (remove low rumble):
  - Cutoff: 80Hz
- Apply RNNoise (AI-based noise reduction):
  - Available via NuGet: RNNoise.NET
  - Or implement using NAudio's ISampleProvider
- Process in real-time before Whisper transcription
- Toggle on/off in settings
- Preview mode to test effectiveness

**Settings:**
```
?? Enable Noise Reduction
  Noise Gate Threshold: [===========|====] -40dB
  High-pass Filter: [==============|] 80Hz
  
  [Preview] [Reset to Defaults]
```

**Success Criteria:**
- ? Reduces background noise audibly
- ? Improves transcription accuracy (test with noisy sample)
- ? Configurable threshold
- ? Can toggle on/off
- ? Minimal CPU impact (<5% additional)
- ? Preview before recording

**Time Estimate:** 6-7 days for Phase 3

---

## ?? Phase 4: Quality & Confidence (Week 4)

**Goal:** Help users identify and fix transcription issues

### Feature 26: Transcription Quality Report

**What:** Show confidence scores and highlight uncertain words

**Implementation:**
```
Models/
  ??? TranscriptionSegment.cs (UPDATED)
  ?   - Add Confidence property (0.0-1.0)
  ?   - Add WordConfidences list
  
Services/
  ??? WhisperTranscriptionService.cs (UPDATED)
      - Extract confidence scores from Whisper
      - Tag low-confidence segments
      
UI/
  ??? TranscriptViewer.cs (NEW - enhanced viewer)
  ??? QualityReportDialog.cs (NEW)
```

**Technical Details:**
- Whisper.NET provides token probabilities
- Extract confidence per word/segment:
  - High confidence: >0.8 (green)
  - Medium: 0.5-0.8 (yellow)
  - Low: <0.5 (red, needs review)
- Visual indicators in transcript:
  - Underline low-confidence words
  - Tooltip shows confidence %
- Quality report dialog:
  - Overall session confidence: 87%
  - Low-confidence segments: 12
  - Most uncertain words: "gnoll", "Waterdeep", "Tiamat"
  - Suggested corrections (from character dictionary)

**Quality Report UI:**
```
???????????????????????????????????????????
? ?? Transcription Quality Report         ?
?                                         ?
? Overall Confidence: 87% ??????????      ?
?                                         ?
? Statistics:                             ?
?  • High confidence: 450 segments (85%)  ?
?  • Medium confidence: 65 segments (12%) ?
?  • Low confidence: 15 segments (3%)     ?
?                                         ?
? Segments Needing Review:                ?
?  [00:15:32] "... the null attacks..." (45%)?
?              ^^^^ Did you mean "gnoll"? ?
?                                         ?
?  [00:28:45] "... waterdeep castle..." (52%)?
?              ^^^^^^^^^ Confidence: low  ?
?                                         ?
? [Jump to Segment] [Export Report]      ?
???????????????????????????????????????????
```

**Export Format:**
```markdown
# Transcription Quality Report

**Session:** session_20251126_143000.md
**Date:** 2025-11-26 14:30:00
**Overall Confidence:** 87%

## Statistics
- High confidence: 450 segments (85%)
- Medium confidence: 65 segments (12%)
- Low confidence: 15 segments (3%)

## Segments Needing Review

### [00:15:32] Confidence: 45%
> "... the **null** attacks with his sword..."

**Suggestion:** Did you mean "gnoll"?

---
```

**Success Criteria:**
- ? Confidence scores shown for each segment
- ? Low-confidence words highlighted
- ? Quality report accessible after recording
- ? Can jump to uncertain segments
- ? Export report as markdown
- ? Suggestions from character dictionary

**Time Estimate:** 7-8 days for Phase 4

---

## ?? Phase 5: Advanced AI Features (Week 5-6)

**Goal:** Leverage AI for improved accuracy and summaries

### Feature 13: Character Name Dictionary

**What:** Train on character/NPC names for better transcription accuracy

**Implementation:**
```
Models/
  ??? CharacterDictionary.cs (NEW)
  ??? CharacterEntry.cs (NEW)
  
Services/
  ??? IDictionaryService.cs (NEW)
  ??? CharacterDictionaryService.cs (NEW)
  
UI/
  ??? DictionaryEditorDialog.cs (NEW)
```

**Technical Details:**
- Store character names with pronunciations:
```json
  {
    "characters": [
      {
        "name": "Drizzt Do'Urden",
        "pronunciation": "drizzt doh-ER-den",
        "aliases": ["Drizzt", "drow ranger"],
        "type": "PC",
        "usageCount": 45
      },
      {
        "name": "Waterdeep",
        "pronunciation": "WAH-ter-deep",
        "type": "location",
        "usageCount": 23
      }
    ]
  }
```
- Import from common sources:
  - Forgotten Realms wiki
  - Campaign setting PDFs (manual entry)
  - Past transcripts (auto-suggest)
- Post-processing step:
  - After Whisper transcription, scan for similar words
  - If confidence < 0.6 AND dictionary match found
  - Replace: "waterdeep" ? "Waterdeep"
  - Replace: "drizzed" ? "Drizzt"
- Fuzzy matching (Levenshtein distance)
- Learn from corrections (user confirms)

**Dictionary UI:**
```
???????????????????????????????????????????
? ?? Character Dictionary                 ?
?                                         ?
? [Search: _____________] [+ Add]         ?
?                                         ?
? Characters (5)                          ?
?  ?? Drizzt Do'Urden (PC) - Used 45x    ?
?  ?? Bruenor (PC) - Used 32x            ?
?  ?? Wulfgar (PC) - Used 28x            ?
?  ?? Cattie-brie (PC) - Used 30x        ?
?  ?? Regis (PC) - Used 18x              ?
?                                         ?
? Locations (3)                           ?
?  ?? Waterdeep - Used 23x               ?
?  ?? Icewind Dale - Used 12x            ?
?  ?? Luskan - Used 8x                   ?
?                                         ?
? [Import from...] [Export] [Close]      ?
???????????????????????????????????????????
```

**Add/Edit Dialog:**
```
???????????????????????????????????????????
? Add Character                           ?
?                                         ?
? Name: [Drizzt Do'Urden___________]     ?
?                                         ?
? Type: (•) PC  ( ) NPC  ( ) Location    ?
?                                         ?
? Pronunciation (optional):               ?
? [drizzt doh-ER-den_______________]     ?
?                                         ?
? Common misspellings:                    ?
? • drizzed                               ?
? • drist                                 ?
? [+ Add more]                            ?
?                                         ?
? [Save] [Cancel]                         ?
???????????????????????????????????????????
```

**Success Criteria:**
- ? Can add/edit character names
- ? Fuzzy matching finds similar words
- ? Auto-corrects common misspellings
- ? Imports from file (JSON/CSV)
- ? Learns from user corrections
- ? Significantly improves accuracy for fantasy names

---

### Feature 16: AI Session Summary

**What:** Generate automatic session recap using AI

**Implementation:**
```
Services/
  ??? ISessionSummaryService.cs (NEW)
  ??? SessionSummaryService.cs (NEW)
  
External:
  - Use local LLM (Phi-3 or Llama 3)
  - Or Claude API (if user provides key)
```

**Technical Details:**
- After recording stops, offer to generate summary
- Options:
  - **Local AI (Free):** Use ONNX Runtime + Phi-3 Mini
  - **Cloud AI (Better quality):** Use Claude API with user's key
- Prompt structure:
```
  You are analyzing a D&D session transcript.
  
  Generate a session summary with these sections:
  1. What Happened (2-3 paragraphs)
  2. Key Decisions Made
  3. Combat Encounters
  4. NPCs Met
  5. Loot/Items Acquired
  6. Unresolved Plot Threads
  7. Next Session Setup
  
  Transcript:
  [full transcript here]
```
- Generate summary in <30 seconds
- Save as separate file: `session_20251126_143000_SUMMARY.md`

**Summary Format:**
```markdown
# Session Summary - November 26, 2025

## What Happened
The party continued their investigation in Waterdeep, 
following leads about the missing nobles. They discovered 
that the Zhentarim are involved and tracked suspects to 
a warehouse in the Dock Ward...

## Key Decisions
- Decided to infiltrate rather than confront directly
- Alice (Wizard) used disguise magic to pose as merchant
- Party agreed to spare the guard captain in exchange for info

## Combat Encounters
- **Warehouse Fight:** 4 Zhentarim thugs, 1 mage
  - Duration: ~15 minutes
  - Casualties: None (party victorious)
  - Key moment: Bob's critical hit saved Alice

## NPCs Met
- **Captain Vrandas:** Zhentarim guard (now informant)
- **Mysterious Hooded Figure:** Escaped, identity unknown

## Loot/Items
- 150 gold pieces
- Potion of Healing (×2)
- Mysterious sealed letter (not yet opened)

## Unresolved Plot Threads
- Who is the hooded figure?
- What's in the sealed letter?
- Are the missing nobles still alive?

## Next Session
Party plans to read the letter and decide whether to 
trust Captain Vrandas. Warehouse location may hold more clues.
```

**Summary UI:**
```
???????????????????????????????????????????
? ?? Generate AI Summary                  ?
?                                         ?
? Would you like to generate an AI        ?
? summary of this session?                ?
?                                         ?
? AI Model:                               ?
? (•) Local AI (Free, ~30 seconds)       ?
? ( ) Cloud AI (Better, requires API key)?
?                                         ?
? [Generate Summary] [Skip]               ?
???????????????????????????????????????????

// After generation:
???????????????????????????????????????????
? ? Summary Generated                    ?
?                                         ?
? Session summary created successfully!   ?
?                                         ?
? [View Summary] [Save to File] [Close]  ?
???????????????????????????????????????????
```

**Success Criteria:**
- ? Generates coherent session summary
- ? Identifies key events, NPCs, combat
- ? Works offline with local AI
- ? Optional cloud AI for better quality
- ? Completes in <60 seconds
- ? Saves as separate markdown file

**Time Estimate:** 10-12 days for Phase 5

---

## ?? Overall Timeline

| Phase | Duration | Features | Completion Date |
|-------|----------|----------|-----------------|
| 1 | Week 1 | Timestamp Markers, Timer | Day 7 |
| 2 | Week 2 | Audio Monitoring, Crash Recovery | Day 14 |
| 3 | Week 3 | Speaker Naming, Noise Reduction | Day 21 |
| 4 | Week 4 | Quality Report | Day 28 |
| 5 | Week 5-6 | Character Dictionary, AI Summary | Day 42 |

**Total Estimated Time:** 6 weeks (42 days)

---

## ?? Technical Dependencies

**NuGet Packages to Add:**
```xml
<!-- Phase 3: Noise Reduction -->
<PackageReference Include="RNNoise.NET" Version="1.0.0" />

<!-- Phase 5: Local AI -->
<PackageReference Include="Microsoft.ML.OnnxRuntime" Version="1.19.0" />
<PackageReference Include="Microsoft.ML.OnnxRuntimeGenAI" Version="0.4.0" />

<!-- Phase 5: Cloud AI (optional) -->
<PackageReference Include="Anthropic.SDK" Version="0.1.0" />
```

**External Models:**
- **Phase 5:** Phi-3 Mini ONNX (~2GB download)
  - Download: https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-onnx

---

## ?? Success Metrics

**After All Phases Complete:**
- ? Zero session data loss (crash recovery)
- ? 95%+ transcription accuracy with character dictionary
- ? Users can review and correct low-confidence segments
- ? Audio quality monitoring prevents recording failures
- ? Quick speaker naming improves workflow
- ? AI summaries save 30+ minutes of manual note-taking

---

## ?? Phase 0: Setup (Before Starting)

**Project Structure Updates:**
```
Chronicis.CaptureApp/
??? Models/
?   ??? TimestampMarker.cs (NEW - Phase 1)
?   ??? RecoverySession.cs (NEW - Phase 2)
?   ??? CharacterDictionary.cs (NEW - Phase 5)
?   ??? TranscriptionSegment.cs (UPDATED - Phase 4)
??? Services/
?   ??? IMarkerService.cs (NEW - Phase 1)
?   ??? MarkerService.cs (NEW - Phase 1)
?   ??? IAudioLevelMonitor.cs (NEW - Phase 2)
?   ??? AudioLevelMonitor.cs (NEW - Phase 2)
?   ??? ICrashRecoveryService.cs (NEW - Phase 2)
?   ??? CrashRecoveryService.cs (NEW - Phase 2)
?   ??? INoiseReductionService.cs (NEW - Phase 3)
?   ??? NoiseReductionService.cs (NEW - Phase 3)
?   ??? IDictionaryService.cs (NEW - Phase 5)
?   ??? CharacterDictionaryService.cs (NEW - Phase 5)
?   ??? ISessionSummaryService.cs (NEW - Phase 5)
?   ??? SessionSummaryService.cs (NEW - Phase 5)
??? UI/
?   ??? SpeakerNameDialog.cs (NEW - Phase 3)
?   ??? DictionaryEditorDialog.cs (NEW - Phase 5)
?   ??? QualityReportDialog.cs (NEW - Phase 4)
?   ??? MainForm.cs (UPDATED - All phases)
??? External/
    ??? Models/
        ??? phi-3-mini.onnx (Phase 5)
```

---

## ?? Implementation Notes

**Testing Strategy:**
- Unit tests for each service
- Integration tests for recording workflow
- Manual testing with real D&D session audio
- Performance profiling for real-time features

**Backward Compatibility:**
- All features are additive
- Existing transcripts remain compatible
- Settings migration handled automatically

**Performance Targets:**
- Audio monitoring: <1% CPU
- Noise reduction: <5% CPU
- Crash recovery auto-save: <100ms
- Quality report generation: <5 seconds
- AI summary: <60 seconds

---

## ?? Ready to Start?

We have a complete plan! Should we:
1. **Start with Phase 1** (Timestamp Markers + Timer)
2. **Review/adjust the plan** first
3. **Prioritize differently** based on your needs