using Aura.Application.Models;

namespace Aura.Application.Ports;

public interface IMorningSummarySettingsProvider
{
    MorningSummarySettings GetSettings();
}
