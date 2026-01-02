namespace Chronicis.Shared.Enums;

/// <summary>
/// Defines a user's role within a world, determining their permissions.
/// All members of a world have access to all campaigns within that world.
/// </summary>
public enum WorldRole
{
    /// <summary>
    /// Full control - creates campaigns, arcs, sessions, manages membership, sees all public content.
    /// Cannot see private content from other users.
    /// </summary>
    GM = 0,
    
    /// <summary>
    /// Can create characters, session notes, contribute to Wiki.
    /// Can mark own content as Private. Cannot create Campaigns or Arcs.
    /// </summary>
    Player = 1,
    
    /// <summary>
    /// Read-only access to public content. Cannot create or edit anything.
    /// </summary>
    Observer = 2
}
