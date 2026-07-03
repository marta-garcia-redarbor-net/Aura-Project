namespace Aura.Domain.FocusState;

/// <summary>
/// Core domain entity representing a user's focus state.
/// Encapsulates state transitions with invariant guards following the WorkItem pattern.
/// </summary>
public sealed class FocusState
{
    /// <summary>Gets the current focus state. Only modifiable through guarded transition methods.</summary>
    public FocusStateType CurrentState { get; private set; }

    /// <summary>
    /// Initializes a new instance of <see cref="FocusState"/>.
    /// Default state is <see cref="FocusStateType.WindowOfOpportunity"/>.
    /// </summary>
    public FocusState()
    {
        CurrentState = FocusStateType.WindowOfOpportunity;
    }

    /// <summary>Transition to <see cref="FocusStateType.DeepWork"/> from <see cref="FocusStateType.Away"/> or <see cref="FocusStateType.Recovery"/>.</summary>
    public void TryEnterDeepWork()
    {
        GuardTransition(FocusStateType.DeepWork);
        CurrentState = FocusStateType.DeepWork;
    }

    /// <summary>Transition to <see cref="FocusStateType.Away"/> from <see cref="FocusStateType.WindowOfOpportunity"/>.</summary>
    public void GoToAway()
    {
        GuardTransition(FocusStateType.Away);
        CurrentState = FocusStateType.Away;
    }

    /// <summary>Transition to <see cref="FocusStateType.Recovery"/> from <see cref="FocusStateType.Away"/>.</summary>
    public void GoToRecovery()
    {
        GuardTransition(FocusStateType.Recovery);
        CurrentState = FocusStateType.Recovery;
    }

    /// <summary>
    /// Transition to <see cref="FocusStateType.WindowOfOpportunity"/> from
    /// <see cref="FocusStateType.DeepWork"/> or <see cref="FocusStateType.Recovery"/>.
    /// </summary>
    public void GoToWindowOfOpportunity()
    {
        GuardTransition(FocusStateType.WindowOfOpportunity);
        CurrentState = FocusStateType.WindowOfOpportunity;
    }

    private void GuardTransition(FocusStateType target)
    {
        if (!IsValidTransition(CurrentState, target))
            throw new InvalidOperationException(
                $"Cannot transition from {CurrentState} to {target}");
    }

    private static bool IsValidTransition(FocusStateType from, FocusStateType to) => (from, to) switch
    {
        (FocusStateType.DeepWork, FocusStateType.WindowOfOpportunity) => true,
        (FocusStateType.WindowOfOpportunity, FocusStateType.Away) => true,
        (FocusStateType.Away, FocusStateType.Recovery) => true,
        (FocusStateType.Away, FocusStateType.DeepWork) => true,
        (FocusStateType.Recovery, FocusStateType.DeepWork) => true,
        (FocusStateType.Recovery, FocusStateType.WindowOfOpportunity) => true,
        _ => false,
    };
}
