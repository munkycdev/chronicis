namespace Chronicis.Shared.Enums;

/// <summary>
/// Defines the lifecycle status of a quest within a campaign.
/// </summary>
public enum QuestStatus
{
    /// <summary>
    /// Quest is currently active and in progress.
    /// </summary>
    Active = 0,
    
    /// <summary>
    /// Quest has been successfully completed.
    /// </summary>
    Completed = 1,
    
    /// <summary>
    /// Quest has failed (objectives cannot be achieved).
    /// </summary>
    Failed = 2,
    
    /// <summary>
    /// Quest has been abandoned by the party.
    /// </summary>
    Abandoned = 3
}
