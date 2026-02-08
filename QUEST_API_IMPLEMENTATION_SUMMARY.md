# Quest API Implementation Summary - Slice 1 (Backend)

**Date:** February 7, 2026  
**Status:** ✅ COMPLETE - Ready for Build Verification

---

## Implementation Overview

This document summarizes the backend-only implementation of Quest and QuestUpdate API endpoints for Chronicis, following the approved architecture in `ARC_QUESTS_ARCHITECTURE.md`.

---

## Files Created

### Core Infrastructure
1. **`src/Chronicis.API/Models/ServiceResult.cs`** (71 lines)
   - Minimal result wrapper with explicit `ServiceStatus` enum
   - Eliminates ambiguity between NotFound/Forbidden/Conflict scenarios
   - Static factory methods for Success, NotFound, Forbidden, Conflict, ValidationError

### Quest Services
2. **`src/Chronicis.API/Services/IQuestService.cs`** (36 lines)
   - Interface defining 5 quest operations

3. **`src/Chronicis.API/Services/QuestService.cs`** (354 lines)
   - `GetQuestsByArcAsync` - Lists quests with GM-only filtering
   - `GetQuestAsync` - Single quest with update count
   - `CreateQuestAsync` - GM-only creation with validation
   - `UpdateQuestAsync` - GM-only update with RowVersion concurrency
   - `DeleteQuestAsync` - GM-only deletion with cascade

### QuestUpdate Services
4. **`src/Chronicis.API/Services/IQuestUpdateService.cs`** (37 lines)
   - Interface defining 3 quest update operations

5. **`src/Chronicis.API/Services/QuestUpdateService.cs`** (264 lines)
   - `GetQuestUpdatesAsync` - Paginated timeline (skip/take)
   - `CreateQuestUpdateAsync` - GM/Player creation (Observer excluded) with SessionId validation
   - `DeleteQuestUpdateAsync` - GM can delete any, Player can delete own

### Controllers
6. **`src/Chronicis.API/Controllers/QuestsController.cs`** (156 lines)
   - `GET /arcs/{arcId}/quests` - List quests for arc
   - `POST /arcs/{arcId}/quests` - Create quest (GM only)
   - `GET /quests/{questId}` - Get single quest
   - `PUT /quests/{questId}` - Update quest (GM only, RowVersion required)
   - `DELETE /quests/{questId}` - Delete quest (GM only)

7. **`src/Chronicis.API/Controllers/QuestUpdatesController.cs`** (111 lines)
   - `GET /quests/{questId}/updates?skip=0&take=20` - Paginated updates
   - `POST /quests/{questId}/updates` - Create update (GM/Player)
   - `DELETE /quests/{questId}/updates/{updateId}` - Delete update (GM any, Player own)

### Configuration
8. **`src/Chronicis.API/Program.cs`** (modified)
   - Added service registrations:
     ```csharp
     builder.Services.AddScoped<IQuestService, QuestService>();
     builder.Services.AddScoped<IQuestUpdateService, QuestUpdateService>();
     ```

---

## Implementation Details

### Authorization Pattern
All services follow the same authorization flow:
1. Resolve Arc → Campaign → World chain via EF joins
2. Check `WorldMembers` table for user's role
3. Apply role-based filtering:
   - **GM:** Full access to all quests (including IsGmOnly)
   - **Player:** Can view non-GM quests, can add QuestUpdates, can delete own updates
   - **Observer:** Read-only for non-GM quests, cannot add/delete updates
4. Return explicit `ServiceResult<T>` with status

### GM-Only Filtering
- Happens **at the service layer**, not in controllers or UI
- Non-GM users:
  - Do not see IsGmOnly quests in list results
  - Get 404 NotFound when accessing IsGmOnly quest by ID
  - Cannot create updates on IsGmOnly quests
- No leakage of quest counts or IDs to unauthorized users

### Concurrency Handling (RowVersion)
- Quest entity has SQL `rowversion` column mapped as `[Timestamp]` in EF
- DTO transports as base64 string
- Update flow:
  1. Client sends `QuestEditDto` with `RowVersion` (base64)
  2. Service converts to byte[], attaches as `OriginalValue` on entity
  3. On save, EF checks RowVersion match
  4. If mismatch: `DbUpdateConcurrencyException` caught
  5. Service reloads current state, returns `ServiceResult.Conflict` with current `QuestDto`
  6. Controller returns 409 Conflict with current state in response body

### SessionId Validation
When creating a QuestUpdate with `SessionId`:
1. Verify Article exists
2. Verify Article.Type == ArticleType.Session
3. Verify Article.ArcId matches Quest.ArcId
4. Return 400 BadRequest on any failure

### Quest.UpdatedAt Semantics
`Quest.UpdatedAt` is updated in two scenarios:
1. Quest edit (PUT /quests/{id})
2. QuestUpdate creation (POST /quests/{id}/updates)

This enables "recently active quests" sorting on Arc Overview without joining to QuestUpdates.

### Pagination
- `GetQuestUpdatesAsync` accepts skip/take parameters
- Validation: skip >= 0, take between 1-100
- Returns `PagedResult<T>` with Items, TotalCount, Skip, Take

### Error Handling
Controllers map `ServiceStatus` to HTTP status codes:
- `Success` → 200 OK / 201 Created / 204 NoContent
- `NotFound` → 404 NotFound
- `Forbidden` → 403 Forbidden
- `Conflict` → 409 Conflict (with current state)
- `ValidationError` → 400 BadRequest

---

## API Endpoints

### Arc-Scoped Quest Endpoints
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/arcs/{arcId}/quests` | Member | List all quests for arc (GM sees all, others see non-GmOnly) |
| POST | `/arcs/{arcId}/quests` | GM | Create new quest |

### Quest-Scoped Endpoints
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/quests/{questId}` | Member | Get single quest with update count |
| PUT | `/quests/{questId}` | GM | Update quest (requires RowVersion) |
| DELETE | `/quests/{questId}` | GM | Delete quest and cascade updates |

### QuestUpdate Endpoints
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/quests/{questId}/updates?skip=0&take=20` | Member | Get paginated updates |
| POST | `/quests/{questId}/updates` | GM/Player | Create update (validates SessionId if provided) |
| DELETE | `/quests/{questId}/updates/{updateId}` | GM/Own | Delete update |

---

## Database Schema

### Existing (No Changes)
- `Quests` table already exists with proper configuration:
  - PK: Id (Guid)
  - FK: ArcId → Arcs (CASCADE)
  - FK: CreatedBy → Users (RESTRICT)
  - RowVersion: byte[] (SQL rowversion)
  - Indexes: (ArcId), (ArcId, Status), (ArcId, UpdatedAt)

- `QuestUpdates` table already exists with proper configuration:
  - PK: Id (Guid)
  - FK: QuestId → Quests (CASCADE)
  - FK: SessionId → Articles (SET NULL)
  - FK: CreatedBy → Users (RESTRICT)
  - Indexes: (QuestId, CreatedAt), (SessionId)

Migration: `20260206000000_AddQuestEntities.cs` already applied

---

## Testing Checklist

### Authorization Tests
- [ ] GM can create/edit/delete quests
- [ ] Player cannot create/edit/delete quests (403)
- [ ] Observer cannot create/edit/delete quests (403)
- [ ] GM can see IsGmOnly quests
- [ ] Player cannot see IsGmOnly quests (404, not in lists)
- [ ] Non-members get 404 for all operations

### QuestUpdate Tests
- [ ] GM can create updates on any quest
- [ ] Player can create updates on non-GmOnly quests
- [ ] Observer cannot create updates (403)
- [ ] Player cannot create updates on IsGmOnly quests (404)
- [ ] GM can delete any update
- [ ] Player can delete only own updates (403 for others')
- [ ] Observer cannot delete updates (403)

### Concurrency Tests
- [ ] Quest edit with stale RowVersion returns 409
- [ ] 409 response includes current QuestDto with latest RowVersion
- [ ] Client can retry with updated RowVersion

### SessionId Validation Tests
- [ ] QuestUpdate with valid SessionId succeeds
- [ ] QuestUpdate with non-existent SessionId returns 400
- [ ] QuestUpdate with non-Session Article returns 400
- [ ] QuestUpdate with Session from different Arc returns 400

### UpdatedAt Timestamp Tests
- [ ] Quest.UpdatedAt updates on quest edit
- [ ] Quest.UpdatedAt updates when QuestUpdate is created

### Pagination Tests
- [ ] skip < 0 returns 400
- [ ] take < 1 or > 100 returns 400
- [ ] PagedResult includes correct TotalCount, Skip, Take

---

## Next Steps

1. **Stop running API** (PID 79676)
2. **Run build script:**
   ```powershell
   Z:\repos\chronicis\build-and-verify-quest-api.ps1
   ```
3. **Verify clean build** (no errors/warnings)
4. **Manual testing** via Postman/curl or client integration
5. **Proceed to Slice 2:** Arc Overview UI implementation

---

## Notes

- ✅ No UI/JS/CSS changes in this slice
- ✅ No schema changes (entities already exist)
- ✅ No new NuGet packages
- ✅ All routing uses explicit templates (no [controller] magic)
- ✅ ServiceResult pattern provides explicit outcomes
- ✅ GM-only filtering at service layer prevents leakage
- ✅ RowVersion concurrency properly handled
- ✅ SessionId validation enforces Arc-scoped integrity
- ✅ Quest.UpdatedAt updated on both edit and update append

---

## Architecture Compliance

This implementation strictly follows:
- ✅ `ARC_QUESTS_ARCHITECTURE.md` v1.1 (all required changes applied)
- ✅ Existing Chronicis authorization patterns
- ✅ Explicit routing (no `/api` prefix, matches existing controllers)
- ✅ ServiceResult pattern for unambiguous outcomes
- ✅ RowVersion concurrency with 409 + current state
- ✅ GM-only filtering at service layer
- ✅ SessionId validation (exists, type=Session, same Arc)
- ✅ Quest.UpdatedAt updated on edit + update append
- ✅ Observer read-only (no QuestUpdate creation)

---

**Implementation completed by:** Claude (Anthropic)  
**Date:** February 7, 2026  
**Slice:** 1 of 4 (Backend Only)
