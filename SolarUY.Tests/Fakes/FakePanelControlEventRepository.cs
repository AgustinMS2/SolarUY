using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Repositories;

namespace SolarUY.Tests.Fakes;

public class FakePanelControlEventRepository : IPanelControlEventRepository
{
    /// <summary>
    /// Eventos de control "persistidos" por el fake (spec 032).
    /// </summary>
    public List<PanelControlEvent> PersistedEvents { get; } = [];

    public void Add(PanelControlEvent controlEvent)
    {
        PersistedEvents.Add(controlEvent);
    }
}
