namespace Aura.UI.Services;

/// <summary>
/// Scoped service that shares demo UI state between the PriorityDashboard page
/// and the Header component. Registered as Scoped so it lives for the circuit.
/// </summary>
public sealed class DemoUiState
{
    public bool IsDemoUser { get; set; }
    public bool SimulationRunning { get; set; }
    public string? SimulationMessage { get; set; }
    public bool DemoDataExists { get; set; }
    public bool ShowWizard { get; set; }        // Shows "Click Demo Mode" wizard
    public bool ShowResetHint { get; set; }     // Shows "Click Reset" wizard (after demo started)

    public event Func<Task>? OnStateChanged;
    public async Task NotifyStateChangedAsync()
    {
        if (OnStateChanged is not null)
            await OnStateChanged.Invoke();
    }
}
