# Phase 1.5.1b: World & Campaign Management APIs

**Status:** ✅ COMPLETE  
**Date:** December 21, 2025  
**Duration:** ~1 hour

---

## Overview

Phase 1.5.1b adds the backend APIs for managing Worlds and Campaigns, enabling users to manually create campaigns within their worlds. This builds on the foundation established in Phase 1.5.1a (Guid migration and entity creation).

---

## What Was Built

### New DTOs

**Chronicis.Shared/DTOs/WorldDtos.cs:**
- `WorldDto` - Basic world info for lists (Id, Name, Description, OwnerId, OwnerName, CreatedAt, CampaignCount)
- `WorldDetailDto` - Extended with Campaigns list
- `WorldCreateDto` - For creating worlds (Name, Description)
- `WorldUpdateDto` - For updating worlds

**Chronicis.Shared/DTOs/CampaignDtos.cs:**
- `CampaignDto` - Basic campaign info (Id, WorldId, Name, Description, OwnerId, etc.)
- `CampaignDetailDto` - Extended with Members list
- `CampaignCreateDto` - For creating campaigns (WorldId, Name, Description)
- `CampaignUpdateDto` - For updating campaigns
- `CampaignMemberDto` - Member info (UserId, DisplayName, Email, Role, JoinedAt, CharacterName)
- `CampaignMemberAddDto` - For adding members (Email, Role, CharacterName)
- `CampaignMemberUpdateDto` - For updating member role

### New Services

**Chronicis.Api/Services/IWorldService.cs & WorldService.cs:**
- `GetUserWorldsAsync(userId)` - Get all worlds user owns or is member of
- `GetWorldAsync(worldId, userId)` - Get world with campaigns
- `CreateWorldAsync(dto, userId)` - Create world with root structure
- `UpdateWorldAsync(worldId, dto, userId)` - Update world
- `UserHasAccessAsync(worldId, userId)` - Check world access
- `UserOwnsWorldAsync(worldId, userId)` - Check world ownership

**Chronicis.Api/Services/ICampaignService.cs & CampaignService.cs:**
- `GetCampaignAsync(campaignId, userId)` - Get campaign with members
- `CreateCampaignAsync(dto, userId)` - Create campaign with auto-creation logic
- `UpdateCampaignAsync(campaignId, dto, userId)` - Update campaign
- `AddMemberAsync(campaignId, dto, requestingUserId)` - Add member by email
- `UpdateMemberAsync(campaignId, userId, dto, requestingUserId)` - Update member role
- `RemoveMemberAsync(campaignId, userId, requestingUserId)` - Remove member
- `GetUserRoleAsync(campaignId, userId)` - Get user's role
- `UserHasAccessAsync(campaignId, userId)` - Check campaign access
- `UserIsDungeonMasterAsync(campaignId, userId)` - Check if user is DM

### New Azure Functions

**Chronicis.Api/Functions/WorldFunctions.cs:**
| Function | Method | Route | Description |
|----------|--------|-------|-------------|
| GetWorlds | GET | `/api/worlds` | List user's worlds |
| GetWorld | GET | `/api/worlds/{id:guid}` | Get world with campaigns |
| CreateWorld | POST | `/api/worlds` | Create new world |
| UpdateWorld | PUT | `/api/worlds/{id:guid}` | Update world |

**Chronicis.Api/Functions/CampaignFunctions.cs:**
| Function | Method | Route | Description |
|----------|--------|-------|-------------|
| GetCampaign | GET | `/api/campaigns/{id:guid}` | Get campaign with members |
| CreateCampaign | POST | `/api/campaigns` | Create campaign |
| UpdateCampaign | PUT | `/api/campaigns/{id:guid}` | Update campaign |
| AddCampaignMember | POST | `/api/campaigns/{id:guid}/members` | Add member |
| UpdateCampaignMember | PUT | `/api/campaigns/{campaignId:guid}/members/{userId:guid}` | Update member |
| RemoveCampaignMember | DELETE | `/api/campaigns/{campaignId:guid}/members/{userId:guid}` | Remove member |

---

## Auto-Creation Logic

When a campaign is created via `POST /api/campaigns`:

1. **Campaign entity** created in Campaigns table
2. **CampaignMember** created with creator as DM (Role = CampaignRole.DM)
3. **Campaign article** (Type = Campaign) created under CampaignRoot
4. **Act 1 article** (Type = Act) created under Campaign article
5. **SharedInfoRoot article** (Type = SharedInfoRoot) created under Act 1

This ensures every campaign has the required structure from the start.

---

## Permission Model

| Action | Who Can Do It |
|--------|---------------|
| Create World | Any authenticated user |
| Update World | World owner only |
| Create Campaign | World owner only |
| Update Campaign | Campaign DM only |
| Add/Remove/Update Members | Campaign DM only |
| Leave Campaign | Any member (self-removal) |
| Remove Last DM | **Blocked** (prevents orphaned campaigns) |

---

## Files Created

| File | Purpose |
|------|---------|
| `src/Chronicis.Shared/DTOs/WorldDtos.cs` | World-related DTOs |
| `src/Chronicis.Shared/DTOs/CampaignDtos.cs` | Campaign-related DTOs |
| `src/Chronicis.Api/Services/IWorldService.cs` | World service interface |
| `src/Chronicis.Api/Services/WorldService.cs` | World service implementation |
| `src/Chronicis.Api/Services/ICampaignService.cs` | Campaign service interface |
| `src/Chronicis.Api/Services/CampaignService.cs` | Campaign service implementation |
| `src/Chronicis.Api/Functions/WorldFunctions.cs` | World API endpoints |
| `src/Chronicis.Api/Functions/CampaignFunctions.cs` | Campaign API endpoints |

## Files Modified

| File | Changes |
|------|---------|
| `src/Chronicis.Api/Program.cs` | Registered IWorldService and ICampaignService |

---

## Key Implementation Details

### JSON Case Sensitivity
Azure Functions with `HttpRequestData.ReadFromJsonAsync` is case-sensitive by default. To accept camelCase JSON from JavaScript clients, we use:
```csharp
private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

var dto = await JsonSerializer.DeserializeAsync<CampaignCreateDto>(req.Body, _jsonOptions);
```

### CampaignRole Enum
The enum uses `DM` not `DungeonMaster`:
```csharp
public enum CampaignRole
{
    DM = 0,
    Player = 1,
    Observer = 2
}
```

### World Access Check
Users have access to a world if they:
1. Own the world, OR
2. Are a member of any campaign in that world

---

## Testing

### Get User's Worlds
```powershell
$token = "YOUR_AUTH_TOKEN"
Invoke-RestMethod -Uri "http://localhost:7071/api/worlds" -Headers @{"X-Auth0-Token"=$token}
```

### Create Campaign
```powershell
$body = @{
    worldId = "YOUR_WORLD_ID"
    name = "Dragon Heist"
    description = "A Waterdeep adventure"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:7071/api/campaigns" -Method Post -Headers @{"X-Auth0-Token"=$token; "Content-Type"="application/json"} -Body $body
```

### Get Campaign Details
```powershell
Invoke-RestMethod -Uri "http://localhost:7071/api/campaigns/CAMPAIGN_ID" -Headers @{"X-Auth0-Token"=$token}
```

---

## What's Next

Phase 1.5.1c (if needed) or Phase 1.5.2:
- Frontend World/Campaign selector dropdowns
- Article type handling in UI
- Context-aware article creation

---

## Lessons Learned

1. **JSON Case Sensitivity:** Azure Functions isolated worker model requires explicit `PropertyNameCaseInsensitive = true` for JSON deserialization
2. **Enum Naming:** Always check existing enum values before using them in new code
3. **Service Registration:** Don't forget to register new services in Program.cs

---

*End of Phase 1.5.1b Documentation*
