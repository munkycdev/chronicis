namespace Chronicis.Shared.Enums;

/// <summary>
/// Defines a user's role within a campaign, determining their permissions.
/// </summary>
public enum CampaignRole
{
    /// <summary>
    /// Full control - creates structure (Acts, Sessions), manages membership, sees all public content.
    /// Cannot see private content from other users.
    /// </summary>
    GM = 0,
    
    /// <summary>
    /// Can create characters, session notes, contribute to Wiki and Shared Info.
    /// Can mark own content as Private. Cannot create Acts or Sessions.
    /// </summary>
    Player = 1,
    
    /// <summary>
    /// Read-only access to public content. Cannot create or edit anything.
    /// </summary>
    Observer = 2
}
