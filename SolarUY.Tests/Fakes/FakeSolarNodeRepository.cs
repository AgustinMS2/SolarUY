using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Repositories;

namespace SolarUY.Tests.Fakes;

public class FakeSolarNodeRepository : ISolarNodeRepository
{
    public List<SolarNode> PersistedNodes { get; } = [];

    public bool ExistsByName(Guid parkId, string normalizedName)
    {
        return PersistedNodes.Any(n => n.SolarParkId == parkId && n.NormalizedName == normalizedName);
    }

    public void Add(SolarNode node)
    {
        PersistedNodes.Add(node);
    }

    public SolarNode? GetById(Guid id)
    {
        return PersistedNodes.FirstOrDefault(n => n.Id == id);
    }
}