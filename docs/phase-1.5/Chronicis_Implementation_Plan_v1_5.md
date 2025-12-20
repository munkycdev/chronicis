# Chronicis Implementation Plan v1.5 - Multi-User Collaboration

**Version:** 1.5.0 | **Date:** December 15, 2025  
**Purpose:** Transform Chronicis from single-user POC to multi-user collaborative tool for gaming groups

**Status:** Planning Phase

---

## Executive Summary

**Goal:** Make Chronicis useful for D&D gaming groups by adding multi-user support, role-based permissions, article types, and collaborative workflows.

**What's New in v1.5:**
- World-based isolation with shared Wiki
- Campaign membership with roles (DM/Player/Observer)
- Article type system (Session, Character, Wiki, etc.)
- Privacy controls (public/private articles)
- Required Act structure for campaign organization
- Cross-campaign character persistence
- Collaborative spaces (Wiki, Shared Information)

**Timeline:** 8 weeks (6 phases)

**Foundation:** Builds on completed POC (Phases 0-9.5)

---

## Quick Navigation

- [Architecture Overview](#architecture-overview)
- [Phase 1.5.1: Multi-User Foundation](#phase-151-multi-user-foundation)
- [Phase 1.5.2: Article Types & Metadata](#phase-152-article-types--metadata)
- [Phase 1.5.3: Permissions & Visibility](#phase-153-permissions--visibility)
- [Phase 1.5.4: Character Sheets](#phase-154-character-sheets)
- [Phase 1.5.5: Session Management](#phase-155-session-management)
- [Phase 1.5.6: Collaboration Polish](#phase-156-collaboration-polish)
- [Migration Strategy](#migration-strategy)
- [Testing Strategy](#testing-strategy)

---

## Architecture Overview

### Core Concepts

**World = Isolation Boundary**
- Each World has its own Wiki, Campaigns, and Characters
- DMs create separate Worlds for different settings (e.g., "Forgotten Realms" vs "Eberron")
- All content within a World is accessible to campaign members
- Provides complete data separation when needed

**Campaign = Gaming Group**
- Multiple users collaborate within a Campaign
- Role-based access (DM, Player, Observer)
- Sequential campaigns within a World share resources (Wiki, Characters)
- Each campaign has its own Act/Session structure

**Required Hierarchy**
```
World
├─ Wiki (collaborative, world-scoped)
│  └─ [infinitely nested articles]
│
├─ Campaigns
│  └─ Campaign
│     └─ Act (required, minimum 1 per campaign)
│        ├─ Sessions
│        │  └─ Session (DM canonical note)
│        │     └─ [Session notes from DM/players]
│        └─ Shared Information
│           └─ [collaborative articles]
│
└─ Characters (world-scoped, persist across campaigns)
   └─ Character
      └─ [infinitely nested notes]
```

### Privacy Model

**Absolute Privacy:**
- Private articles visible ONLY to owner (no DM override)
- Enables player secrets, DM prep, character motivations
- Complete privacy from all other users including DM

**Permission-Aware Features:**
- Search includes user's own private articles (visually distinct with 🔒)
- Backlinks show user's own private references (hidden from others)
- AI summaries personalized by what user can access
- Hashtags in private articles don't create visible backlinks for others

### Article Type System

**Structural Containers (Auto-Created):**
- WorldRoot - Top-level world container
- WikiRoot - Wiki container (world-scoped)
- CampaignRoot - Campaigns container
- Campaign - Individual campaign
- Act - Story arc (required, minimum 1)
- CharacterRoot - Characters container
- SharedInfoRoot - Shared Information container (per-act)

**Content Articles (User-Created):**
- WikiArticle - Locations, NPCs, Items in Wiki
- WorldNote - DM's private notes in World space
- Session - Session article (IS the DM's canonical note)
- SessionNote - Player or DM child article under Session
- SharedInfo - Collaborative articles under Shared Information
- Character - Top-level character (world-scoped)
- CharacterNote - Nested under character

---

## Phase 1.5.1: Multi-User Foundation

**Duration:** 2 weeks  
**Goal:** Establish World/Campaign/User relationships and basic multi-user infrastructure

### New Entities

**World**
```csharp
public class World
{
    public Guid Id { get; set; }
    public string Name { get; set; }        // Max 200 chars
    public Guid OwnerId { get; set; }       // FK to User (the DM)
    public DateTime CreatedAt { get; set; }
    
    // Navigation
    public User Owner { get; set; }
    public ICollection<Campaign> Campaigns { get; set; }
}
```

**Campaign**
```csharp
public class Campaign
{
    public Guid Id { get; set; }
    public Guid WorldId { get; set; }       // FK to World
    public string Name { get; set; }        // Max 200 chars
    public string? Description { get; set; } // Max 1000 chars
    public Guid OwnerId { get; set; }       // FK to User (the DM)
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; } // When campaign began IRL
    
    // Navigation
    public World World { get; set; }
    public User Owner { get; set; }
    public ICollection<CampaignMember> Members { get; set; }
}
```

**CampaignMember**
```csharp
public class CampaignMember
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }    // FK to Campaign
    public Guid UserId { get; set; }        // FK to User
    public CampaignRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public string? CharacterName { get; set; } // Max 100 chars, for Players
    
    // Navigation
    public Campaign Campaign { get; set; }
    public User User { get; set; }
}

public enum CampaignRole
{
    DM,        // Full control, creates structure, sees all public content
    Player,    // Can create characters, session notes, contribute to Wiki/Shared Info
    Observer   // Read-only access to public content
}
```

### Article Entity Changes

**New Fields:**
```csharp
public class Article
{
    // Existing fields...
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public string? IconEmoji { get; set; }
    
    // NEW in v1.5
    public Guid? WorldId { get; set; }      // For Wiki articles and root containers
    public Guid? CampaignId { get; set; }   // For campaign-specific articles
    public ArticleType Type { get; set; }   // Determines behavior and permissions
    public ArticleVisibility Visibility { get; set; } // Public or Private
    public Guid CreatedBy { get; set; }     // FK to User
    public Guid? LastModifiedBy { get; set; } // FK to User
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // For sessions
    public DateTime? SessionDate { get; set; }
    public string? InGameDate { get; set; }
    
    // For characters
    public Guid? PlayerId { get; set; }     // Which user owns this character
}

public enum ArticleType
{
    // Structural (auto-created)
    WorldRoot,
    WikiRoot,
    CampaignRoot,
    Campaign,
    Act,
    CharacterRoot,
    SharedInfoRoot,
    
    // Content (user-created)
    WikiArticle,
    WorldNote,
    Session,
    SessionNote,
    SharedInfo,
    Character,
    CharacterNote
}

public enum ArticleVisibility
{
    Public,   // Everyone in campaign can see
    Private   // Only owner can see (absolute privacy)
}
```

### Auto-Creation Logic

**When World is Created:**
1. Create World entity
2. Create WorldRoot article (container)
3. Create WikiRoot article under WorldRoot
4. Create CampaignRoot article under WorldRoot
5. Create CharacterRoot article under WorldRoot

**When Campaign is Created:**
1. Create Campaign entity
2. Add creator as CampaignMember with Role=DM
3. Create Campaign article under CampaignRoot
4. Create Act 1 article under Campaign
5. Create SharedInfoRoot article under Act 1

### Backend API Changes

**New Endpoints:**
- `POST /api/worlds` - Create world
- `GET /api/worlds` - List user's worlds
- `GET /api/worlds/{id}` - Get world with campaigns
- `POST /api/campaigns` - Create campaign (auto-creates Act 1)
- `GET /api/campaigns/{id}` - Get campaign details
- `POST /api/campaigns/{id}/members` - Add member
- `DELETE /api/campaigns/{id}/members/{userId}` - Remove member
- `PUT /api/campaigns/{id}/members/{userId}/role` - Change role

**Modified Endpoints:**
- All article endpoints now require WorldId context
- Tree navigation scoped to current World

### Frontend Changes

**New Components:**
- WorldSelector - Dropdown to switch between worlds
- CampaignSelector - Dropdown to switch campaigns within world
- WorldCreateDialog - Create new world
- CampaignCreateDialog - Create new campaign
- MemberManagement - Add/remove campaign members

**State Changes:**
- Add CurrentWorldId to app state
- Add CurrentCampaignId to app state
- Tree navigation filters by World/Campaign context

### Success Criteria

1. ✅ Can create a World
2. ✅ Can create a Campaign within World
3. ✅ Campaign auto-creates Act 1 and Shared Information
4. ✅ Can add/remove campaign members
5. ✅ Can assign roles (DM/Player/Observer)
6. ✅ World selector works in UI
7. ✅ Campaign selector works in UI
8. ✅ Article tree scoped to current context

---

## Phase 1.5.2: Article Types & Metadata

**Duration:** 1 week  
**Goal:** Implement article type system with type-specific behavior

### Type-Specific Icons

| Type | Icon | Color |
|------|------|-------|
| WorldRoot | 🌍 | Default |
| WikiRoot | 📚 | Default |
| CampaignRoot | ⚔️ | Default |
| Campaign | 🎭 | Default |
| Act | 📖 | Default |
| CharacterRoot | 👥 | Default |
| SharedInfoRoot | 📋 | Default |
| WikiArticle | (user-chosen emoji) | Default |
| WorldNote | 📝 | Muted |
| Session | 🎲 | Default |
| SessionNote | 📄 | Default |
| SharedInfo | 🤝 | Default |
| Character | 🧙 | Default |
| CharacterNote | 📝 | Default |

### Type-Specific Metadata

**Session Articles:**
- SessionDate (DateTime) - Real-world date of session
- InGameDate (string) - In-game calendar date (flexible format)
- Session number (auto-calculated from order in Act)

**Character Articles:**
- PlayerId - Which user owns this character
- CharacterClass, Level, Race (future expansion)

### Context Menu Logic

**What can be created where?**

| Parent Type | User Role | Can Create |
|-------------|-----------|------------|
| WikiRoot | Any | WikiArticle |
| WikiArticle | Any | WikiArticle |
| World (DM only) | DM | WorldNote |
| Act | DM | Session |
| Session | DM/Player | SessionNote |
| SharedInfoRoot | Any | SharedInfo |
| SharedInfo | Any | SharedInfo |
| CharacterRoot | Any | Character |
| Character | Owner | CharacterNote |
| CharacterNote | Owner | CharacterNote |

**Context Menu Display:**
- Show only valid types for current location and role
- Use friendly names ("New Session", not "Create ArticleType.Session")

### Success Criteria

1. ✅ Articles have correct Type assigned on creation
2. ✅ Type-specific icons display in tree
3. ✅ Context menu shows only valid creation options
4. ✅ Session metadata (dates) editable
5. ✅ Type cannot be changed after creation (immutable)

---

## Phase 1.5.3: Permissions & Visibility

**Duration:** 1.5 weeks  
**Goal:** Implement role-based access control and privacy

### Permission Rules

**DM Can:**
- Create/edit/delete all structural articles (Acts, Sessions)
- Create/edit/delete own notes anywhere
- View all public content
- Manage campaign membership
- NOT see private content from players

**Player Can:**
- Create/edit/delete own characters and character notes
- Create/edit/delete own session notes
- Contribute to Wiki (create/edit WikiArticles)
- Contribute to Shared Information
- Mark own content as Private
- NOT see private content from others
- NOT create Acts or Sessions

**Observer Can:**
- View public content only
- NOT create or edit anything

### Visibility Implementation

**Private Articles:**
- Only owner can see in tree navigation
- Only owner sees in search results
- Visual indicator (🔒) in tree
- Backlinks from private articles hidden from others
- AI summaries exclude private content for non-owners

**Visual Treatment:**
```css
.article-private {
    opacity: 0.7;
}
.article-private::before {
    content: "🔒 ";
}
```

### API Permission Middleware

Add to existing `AuthenticationMiddleware`:
```csharp
// After user authentication, check article-level permissions
public async Task<bool> CanAccessArticle(Guid userId, Guid articleId)
{
    var article = await _db.Articles.FindAsync(articleId);
    if (article.Visibility == ArticleVisibility.Public) return true;
    if (article.CreatedBy == userId) return true;
    return false;
}

public async Task<bool> CanEditArticle(Guid userId, Guid articleId)
{
    var article = await _db.Articles.FindAsync(articleId);
    var role = await GetUserRole(userId, article.CampaignId);
    
    // Check ownership or role-based permissions
    // Implementation depends on article type
}
```

### Success Criteria

1. ✅ Private articles only visible to owner
2. ✅ Private indicator (🔒) shows in tree
3. ✅ Search respects visibility
4. ✅ Backlinks respect visibility
5. ✅ API returns 403 for unauthorized access
6. ✅ Role-based creation restrictions enforced
7. ✅ Observers cannot edit anything

---

## Phase 1.5.4: Character Sheets

**Duration:** 1 week  
**Goal:** Implement persistent character system

### Character Features

**World-Scoped Characters:**
- Characters exist at World level, not Campaign level
- Same character can participate in multiple sequential campaigns
- Character notes persist across campaigns

**Character Ownership:**
- PlayerId field links to User
- Only owner can edit character and its notes
- DM can view public character content

**Character Article Structure:**
```
Character (Type=Character, PlayerId=user)
├─ Backstory (Type=CharacterNote)
├─ Goals & Motivations (Type=CharacterNote, Visibility=Private)
├─ Session 1 Notes (Type=CharacterNote)
└─ [infinitely nested]
```

### UI Components

**CharacterList:**
- Grid view of all characters in World
- Filter: Mine / All
- Shows character name, player name, campaign(s)

**CharacterDetail:**
- Large portrait area (future: image upload)
- Character name, class, level, race (metadata)
- Nested notes below
- "This is your character" indicator for owner

### Success Criteria

1. ✅ Can create characters in World
2. ✅ Characters persist across campaigns
3. ✅ Character ownership enforced
4. ✅ Character notes nest correctly
5. ✅ Character list view works
6. ✅ Private character notes hidden from others

---

## Phase 1.5.5: Session Management

**Duration:** 1.5 weeks  
**Goal:** Implement session creation and timeline

### Session Workflow

**Creating a Session:**
1. DM clicks "New Session" in Act
2. System creates Session article (this IS the DM's canonical note)
3. Session auto-numbered based on order in Act
4. DM edits session article with their notes

**Adding Session Notes:**
1. Player clicks "Add Note" on Session
2. Creates SessionNote under Session
3. Player writes their perspective
4. Can mark private or public

**Session Metadata:**
- Session number (auto: Session 1, 2, 3...)
- Real date (when played IRL)
- In-game date (flexible string)

### Session Notes Structure

```
Act 1
├─ Session 1 (DM's canonical note)
│  ├─ Player A's Notes (Public)
│  ├─ Player B's Notes (Private)
│  └─ DM's Private Notes (Private)
├─ Session 2
└─ Session 3
```

### Timeline View (Basic)

- Chronological list of sessions in campaign
- Shows session number, date, title
- Click to navigate to session
- Future: visual timeline with milestones

### Success Criteria

1. ✅ Sessions created under Acts only
2. ✅ Session auto-numbering works
3. ✅ Session metadata editable
4. ✅ Players can add notes to sessions
5. ✅ Session timeline view functional
6. ✅ Private session notes work correctly

---

## Phase 1.5.6: Collaboration Polish

**Duration:** 1 week  
**Goal:** Add collaboration UX polish and activity awareness

### Author Attribution

**Display "Created by" and "Last edited by":**
- Show in article metadata area
- Format: "Created by Dave on Dec 15, 2025"
- Format: "Last edited by Sarah 2 hours ago"

### Activity Indicators

**Dashboard Enhancements:**
- "Recent Activity" widget showing recent edits
- "New since last visit" badges
- Filter by: All / Mine / Campaign

**Tree Navigation:**
- Subtle indicator for recently modified articles
- "New" badge for unread content (future)

### Loading States & Empty States

**Consistent Loading:**
- Skeleton loaders for tree, article detail
- Progress indicators for saves

**Helpful Empty States:**
- "No sessions yet. Click + to create your first session."
- "No characters in this world. Create one to get started!"
- Include action buttons in empty states

### Success Criteria

1. ✅ Author names displayed throughout UI
2. ✅ "Last edited" timestamps visible
3. ✅ Recent activity widget on dashboard
4. ✅ New content badges work correctly
5. ✅ Loading states smooth and professional
6. ✅ Empty states helpful and friendly
7. ✅ Error handling graceful and informative

---

## Migration Strategy

### POC Data State

Existing POC has:
- Article entities with no WorldId, CampaignId, Type, Visibility
- No World, Campaign, or CampaignMember entities
- Single user (from Auth0)

### Migration Steps

1. **Create Default World for Each User:**
   - For each existing Auth0 user, create a World called "My World"
   - Set OwnerId to user's ID
   - Create root structure (WorldRoot, WikiRoot, CampaignRoot, CharacterRoot)

2. **Create Default Campaign:**
   - Create Campaign called "My Campaign" for each user
   - Add user as DM
   - Create Campaign article, Act 1, Shared Information

3. **Migrate Existing Articles:**
   - Set all existing articles to Type = WikiArticle
   - Set WorldId to user's default world
   - Set Visibility = Public
   - Set CreatedBy to article's existing user
   - Move all articles under WikiRoot

4. **Preserve Hierarchy:**
   - Keep existing ParentId relationships
   - Users can manually reorganize later

### Migration Script

```sql
-- Pseudo-SQL for migration logic
-- 1. Create default world for each user
-- 2. Create root structure
-- 3. Update articles with WorldId, Type, Visibility
-- 4. Create default campaign
-- Run as part of v1.5 deployment
```

### Breaking Changes

- Article schema changes (new required fields)
- API endpoints change (now require world/campaign context)
- Tree navigation changes (root containers)

**v1.5 is a major upgrade - not backward compatible with v1.0 clients**

---

## Testing Strategy

### Unit Tests

- World/Campaign/Member CRUD operations
- Article type assignment logic
- Permission checking logic
- Visibility filtering
- Auto-creation workflows

### Integration Tests

- End-to-end world creation flow
- Campaign membership management
- Cross-user visibility tests
- Role-based access tests

### Manual Testing

- Complete user journey as DM
- Complete user journey as Player
- Complete user journey as Observer
- Private article workflows
- Migration verification

---

## Summary

**v1.5 Transform:**
- From: Single-user article hierarchy
- To: Multi-user collaborative knowledge management

**Key Additions:**
- Worlds, Campaigns, Roles, Privacy, Types
- 8 weeks across 6 phases

**Foundation for v1.6+:**
- Templates
- Timelines
- Notifications
- Real-time collaboration

---

**When Ready to Start:**
1. Create implementation chat: "Ready to start Phase 1.5.1 - Multi-User Foundation"
2. Review this spec section before each phase
3. Use GitHub Copilot for implementation
4. Use Claude for architecture questions

**Version Control:**
- Tag v1.0 at end of POC (after Phase 12)
- Tag v1.5.0 at end of Phase 1.5.6
- Branch strategy: feature/phase-1-5-x for each phase

---

*End of v1.5 Implementation Plan*
