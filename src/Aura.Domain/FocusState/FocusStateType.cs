namespace Aura.Domain.FocusState;

/// <summary>
/// Represents the possible focus states a user may be in.
/// </summary>
public enum FocusStateType
{
    /// <summary>User is in deep concentration. No interruptions except critical urgency.</summary>
    DeepWork,

    /// <summary>User is receptive to interruptions. Default state.</summary>
    WindowOfOpportunity,

    /// <summary>User is unavailable (meeting, DND, away from keyboard).</summary>
    Away,

    /// <summary>Post-interruption or post-meeting reacclimation period.</summary>
    Recovery,
}
