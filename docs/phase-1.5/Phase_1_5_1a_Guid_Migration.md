# Phase 1.5.1a: Guid ID Migration & Multi-User Foundation

**Status:** ✅ COMPLETE  
**Completed:** December 20, 2025  
**Implementation Time:** ~4 hours

---

## Overview

Phase 1.5.1a implements the foundational database schema changes required for multi-user collaboration. This phase migrates all entity IDs from `int` to `Guid` and introduces new entities for World, Campaign, and CampaignMember management.

---

## Goals

1. Migrate all entity primary keys from `int` to `Guid` for globally unique identification
2. Add new entities: `World`, `Campaign`, `CampaignMember`
3. Add new enums: `ArticleType`, `CampaignRole`, `ArticleVisibility`
4. Rename date properties for consistency: `CreatedDate` → `CreatedAt`, `ModifiedDate` → `ModifiedAt`
5. Add new Article fields for multi-user context: `WorldId`, `CampaignId`, `Type`, `Visibility`, `SessionDate`, `InGameDate`, `PlayerId`

---

## Database Schema Changes

### New Tables

**Worlds**
| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Guid primary key |
| Name | nvarchar(200) | World name |
| Description | nvarchar(1000) | Optional description |
| OwnerId | uniqueidentifier (FK) | References Users.Id |
| CreatedAt | datetime2 | Creation timestamp |

**Campaigns**
| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Guid primary key |
| WorldId | uniqueidentifier (FK) | References Worlds.Id |
| Name | nvarchar(200) | Campaign name |
| Description | nvarchar(1000) | Optional description |
| OwnerId | uniqueidentifier (FK) | References Users.Id |
| CreatedAt | datetime2 | Creation timestamp |
| StartedAt | datetime2 | Optional start date |
| EndedAt | datetime2 | Optional end date |

**CampaignMembers**
| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Guid primary key |
| CampaignId | uniqueidentifier (FK) | References Campaigns.Id |
| UserId | uniqueidentifier (FK) | References Users.Id |
| Role | int | CampaignRole enum value |
| JoinedAt | datetime2 | Join timestamp |
| CharacterName | nvarchar(100) | Optional character name |

### Modified Tables

**Users**
- `Id`: int → Guid
- `CreatedAt`: renamed from CreatedDate
- `LastLoginAt`: renamed from LastLoginDate

**Articles**
- `Id`: int → Guid
- `ParentId`: int? → Guid?
- `CreatedBy`: int → Guid (renamed from UserId)
- `LastModifiedBy`: Guid? (new field)
- `CreatedAt`: renamed from CreatedDate
- `ModifiedAt`: renamed from ModifiedDate
- `WorldId`: Guid? (new field)
- `CampaignId`: Guid? (new field)
- `Type`: int (new field, ArticleType enum)
- `Visibility`: int (new field, ArticleVisibility enum)
- `SessionDate`: datetime2? (new field)
- `InGameDate`: nvarchar(100) (new field)
- `PlayerId`: Guid? (new field)

**Hashtags**
- `Id`: int → Guid
- `LinkedArticleId`: int? → Guid?
- `CreatedAt`: renamed from CreatedDate

**ArticleHashtags**
- `Id`: int → Guid
- `ArticleId`: int → Guid
- `HashtagId`: int → Guid
- `CreatedAt`: renamed from CreatedDate

---

## New Enums

```csharp
public enum ArticleType
{
    General = 0,
    WikiRoot = 1,
    CampaignRoot = 2,
    CharacterRoot = 3,
    SessionRoot = 4,
    Session = 5,
    Character = 6,
    Location = 7,
    Item = 8,
    Faction = 9,
    Lore = 10
}

public enum CampaignRole
{
    Player = 0,
    DungeonMaster = 1,
    Spectator = 2
}

public enum ArticleVisibility
{
    Public = 0,
    Party = 1,
    DungeonMaster = 2,
    Private = 3
}
```

---

## Files Created

### Shared/Models
- `World.cs` - World entity
- `Campaign.cs` - Campaign entity  
- `CampaignMember.cs` - Campaign membership entity

### Shared/Enums
- `ArticleType.cs`
- `CampaignRole.cs`
- `ArticleVisibility.cs`

### Api/Migrations
- `20251220211327_InitialGuidSchema.cs` - EF Core migration

---

## Files Modified

### Shared Layer
- `Models/Article.cs` - Guid IDs, new fields, renamed properties
- `Models/User.cs` - Guid Id, renamed properties
- `Models/Hashtag.cs` - Guid IDs, renamed properties
- `Models/ArticleHashtag.cs` - Guid IDs, renamed properties
- `DTOs/ArticleDTOs.cs` - All DTOs updated for Guid and new fields
- `DTOs/HashtagDto.cs` - Guid IDs
- `DTOs/BacklinkDto.cs` - Guid IDs
- `DTOs/HashtagPreviewDto.cs` - Guid IDs
- `DTOs/SearchDtos.cs` - Guid IDs
- `DTOs/SummaryDtos.cs` - Guid IDs, renamed properties
- `DTOs/AutoHashtagDtos.cs` - List<Guid> for article IDs

### Api Layer
- `Data/ChronicisDbContext.cs` - Added DbSets for World, Campaign, CampaignMember; updated configurations
- `Services/IArticleService.cs` - Guid parameters, worldId filtering
- `Services/ArticleService.cs` - Guid IDs throughout
- `Services/IUserService.cs` - Guid parameters, CreateDefaultWorldAsync
- `Services/UserService.cs` - Default world creation on first login
- `Services/IHashtagSyncService.cs` - Guid articleId
- `Services/HashtagSyncService.cs` - Guid IDs
- `Services/IAISummaryService.cs` - Guid articleId
- `Services/AISummaryService.cs` - Guid IDs
- `Services/IArticleValidationService.cs` - Guid articleId
- `Services/ArticleValidationService.cs` - Guid IDs
- `Services/IAutoHashtagService.cs` - Guid parameters
- `Services/AutoHashtagService.cs` - Guid IDs
- `Functions/CreateArticle.cs` - Guid.NewGuid(), route constraint
- `Functions/UpdateArticle.cs` - Guid parameter, route constraint
- `Functions/DeleteArticle.cs` - Guid parameter, route constraint
- `Functions/ArticleFunctions.cs` - Guid parameters (already done)
- `Functions/ArticleSearchFunction.cs` - Guid IDs
- `Functions/MoveArticle.cs` - Guid parameters (already done)
- `Functions/GetArticleByPath.cs` - Uses Guid service
- `Functions/HashtagFunctions.cs` - Guid IDs (already done)
- `Functions/BacklinkFunctions.cs` - Guid IDs (already done)
- `Functions/AISummaryFunctions.cs` - Guid IDs (already done)
- `Functions/AutoHashtagFunction.cs` - Guid IDs

### Client Layer
- `Services/IArticleApiService.cs` - Guid parameters (already done)
- `Services/ArticleApiService.cs` - Guid IDs (already done)
- `Services/IHashtagApiService.cs` - Guid articleId
- `Services/HashtagApiService.cs` - Guid IDs
- `Services/IAISummaryApiService.cs` - Guid articleId
- `Services/AISummaryApiService.cs` - Guid IDs
- `Services/IAutoHashtagApiService.cs` - List<Guid>
- `Services/AutoHashtagApiService.cs` - List<Guid>
- `Services/ITreeStateService.cs` - Guid (already done)
- `Services/TreeStateService.cs` - Guid (already done)
- `ViewModels/ArticleTreeItemViewModel.cs` - Guid (already done)
- `Components/Articles/ArticleDetail.razor` - Guid IDs, CreatedAt/ModifiedAt
- `Components/Articles/ArticleTreeView.razor` - Guid IDs, CreatedAt
- `Components/Articles/AISummarySection.razor` - Guid ArticleId, GeneratedAt
- `Components/Articles/BacklinksPanel.razor` - Guid ArticleId
- `Components/Articles/ArticleHashtagsPanel.razor` - Guid ArticleId
- `Components/Articles/AutoTagDialog.razor` - Guid ArticleId, List<Guid>
- `Components/Hashtags/HashtagLinkDialog.razor` - Guid selection
- `Pages/Dashboard.razor` - Guid IDs, CreatedAt
- `Pages/Articles.razor` - Guid comparison
- `Pages/Tools/AutoHashtag.razor` - HashSet<Guid>

---

## Key Implementation Details

### ID Generation
All new entities use `Guid.NewGuid()` for ID generation at creation time, ensuring globally unique identifiers across all instances.

### Property Naming Convention
Standardized on `CreatedAt`, `ModifiedAt`, `GeneratedAt` naming convention for all timestamp fields across the codebase.

### Route Constraints
All API routes with ID parameters now use the `{id:guid}` route constraint for proper Guid parsing.

### Default World Creation
When a user logs in for the first time, `UserService.GetOrCreateUserAsync` now creates:
1. A default World for the user
2. Root article structure within that World

### Foreign Key Cascade Behavior
- Most FKs use `ReferentialAction.Restrict` to prevent accidental cascading deletes
- `CampaignMembers` uses `Cascade` delete (members removed when campaign deleted)
- `ArticleHashtags` uses `Cascade` delete (links removed when article or hashtag deleted)
- `Hashtags.LinkedArticleId` uses `SetNull` on delete (hashtag preserved, just unlinked)

---

## Migration Notes

### Database Preparation
The dev database (`chronicis-db-v15-dev`) was cleared before migration since there's no clean data migration path from int to Guid PKs.

### EF Core Migration
Migration created with:
```powershell
dotnet ef migrations add InitialGuidSchema --output-dir Migrations
dotnet ef database update
```

### Indexes Created
- `IX_Articles_WorldId`
- `IX_Articles_CampaignId`
- `IX_Articles_Type`
- `IX_Articles_CreatedBy`
- `IX_Articles_LastModifiedBy`
- `IX_Articles_PlayerId`
- `IX_CampaignMembers_CampaignId_UserId` (unique)
- `IX_Campaigns_WorldId`
- `IX_Campaigns_OwnerId`
- `IX_Worlds_OwnerId`

---

## Testing

### Build Verification
- ✅ Solution builds with no errors
- ✅ API project builds successfully
- ✅ Client project builds successfully

### Runtime Verification
- ✅ API starts successfully
- ✅ Client starts successfully
- ✅ User authentication works
- ✅ Article creation works
- ✅ Article editing works
- ✅ Tree navigation works

---

## Lessons Learned

1. **Systematic approach is essential** - Updating int→Guid touches virtually every file. Working through layers (Entities → DTOs → Services → Functions → Client) prevents circular dependency issues.

2. **Property renames compound complexity** - Changing `CreatedDate` to `CreatedAt` added significant work on top of the ID migration. Consider doing such renames in a separate phase.

3. **Blazor components need careful review** - Razor files don't always show up in IDE error lists clearly. Manual review of each component was necessary.

4. **HashSet<int> to HashSet<Guid>** - Collection type changes in ViewModels required explicit updates that weren't caught by simple find-replace.

5. **Route constraints matter** - Without `{id:guid}` route constraints, Azure Functions would fail to bind Guid parameters from URL segments.

---

## Next Steps (Phase 1.5.1b and beyond)

- Implement article type handling in UI
- Add visibility controls and permission checking
- Implement World/Campaign selection UI
- Add campaign member management
- Implement session date tracking for session articles

---

*End of Phase 1.5.1a Documentation*
