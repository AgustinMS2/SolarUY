using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Repositories;

namespace SolarUY.Tests.Fakes;

public class FakeSolarPanelRepository : ISolarPanelRepository
{
    public List<SolarPanel> Panels { get; } = [];
    public List<SolarPark> Parks { get; } = [];

    public bool ExistsBySerieNumber(string normalizedSerieNumber)
    {
        return Panels.Any(p => p.NormalizedSerieNumber == normalizedSerieNumber);
    }

    public void Add(SolarPanel panel)
    {
        Panels.Add(panel);
    }

    public SolarPark? GetParkById(Guid parkId)
    {
        return Parks.FirstOrDefault(p => p.Id == parkId);
    }

    public IReadOnlyList<SolarPanel> GetAll()
    {
        return Panels;
    }
}
