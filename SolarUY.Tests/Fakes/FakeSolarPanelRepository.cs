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

    public bool ExistsBySerieNumber(string normalizedSerieNumber, Guid excludeId)
    {
        return Panels.Any(p => p.NormalizedSerieNumber == normalizedSerieNumber && p.Id != excludeId);
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

    public SolarPanel? GetById(Guid id)
    {
        return Panels.FirstOrDefault(p => p.Id == id);
    }

    public void Update(SolarPanel panel)
    {
        var index = Panels.FindIndex(p => p.Id == panel.Id);
        if (index >= 0)
        {
            Panels[index] = panel;
        }
        else
        {
            Panels.Add(panel);
        }
    }
}
