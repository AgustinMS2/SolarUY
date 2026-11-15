using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Repositories;

namespace SolarUY.Tests.Fakes;

public class FakeGridInjectionEventRepository : IGridInjectionEventRepository
{
    /// <summary>
    /// Eventos de inyección "persistidos" por el fake (spec 033).
    /// </summary>
    public List<GridInjectionEvent> PersistedEvents { get; } = [];

    public void Add(GridInjectionEvent injectionEvent)
    {
        PersistedEvents.Add(injectionEvent);
    }
}
