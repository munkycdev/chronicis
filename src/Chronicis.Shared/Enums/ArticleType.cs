namespace Chronicis.Shared.Enums;

/// <summary>
/// Defines the type of article, which determines behavior, permissions, and valid parent/child relationships.
/// </summary>
public enum ArticleType
{
    // ===== Structural Containers (Auto-Created) =====
    
    /// <summary>Top-level world container</summary>
    WorldRoot = 0,
    
    /// <summary>Wiki container (world-scoped)</summary>
    WikiRoot = 1,
    
    /// <summary>Campaigns container</summary>
    CampaignRoot = 2,
    
    /// <summary>Individual campaign</summary>
    Campaign = 3,
    
    /// <summary>Story arc container (required, minimum 1 per campaign)</summary>
    Act = 4,
    
    /// <summary>Characters container (world-scoped)</summary>
    CharacterRoot = 5,
    
    /// <summary>Shared Information container (per-act)</summary>
    SharedInfoRoot = 6,
    
    // ===== Content Articles (User-Created) =====
    
    /// <summary>Locations, NPCs, Items, etc. in Wiki</summary>
    WikiArticle = 100,
    
    /// <summary>DM's private notes in World space</summary>
    WorldNote = 101,
    
    /// <summary>Session article - IS the DM's canonical note</summary>
    Session = 102,
    
    /// <summary>Player or DM child article under Session</summary>
    SessionNote = 103,
    
    /// <summary>Collaborative articles under Shared Information</summary>
    SharedInfo = 104,
    
    /// <summary>Top-level character (world-scoped)</summary>
    Character = 105,
    
    /// <summary>Nested notes under character</summary>
    CharacterNote = 106
}
