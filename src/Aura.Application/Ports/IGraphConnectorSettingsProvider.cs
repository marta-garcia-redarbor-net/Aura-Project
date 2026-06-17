using Aura.Application.Models;

namespace Aura.Application.Ports;

public interface IGraphConnectorSettingsProvider
{
    GraphConnectorSettings GetSettings();
}
