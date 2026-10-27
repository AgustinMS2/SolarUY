using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Repositories;

namespace SolarUY.Tests.Fakes;

public class FakeSolarParkRepository : ISolarParkRepository
{
    public List<SolarPark> Parks { get; } = [];

    public bool ExistsByNormalizedName(string normalizedName)
    {
        return Parks.Any(p => p.NormalizedName == normalizedName);
    }

    public bool ExistsByNormalizedName(string normalizedName, Guid excludeId)
    {
        return Parks.Any(p => p.NormalizedName == normalizedName && p.Id != excludeId);
    }

    public void Add(SolarPark park)
    {
        Parks.Add(park);
    }

    public IReadOnlyList<SolarPark> GetAll()
    {
        return Parks;
    }

    public SolarPark? GetById(Guid id)
    {
        return Parks.FirstOrDefault(p => p.Id == id);
    }

    public void Update(SolarPark park)
    {
        var index = Parks.FindIndex(p => p.Id == park.Id);
        if (index >= 0)
        {
            Parks[index] = park;
        }
        else
        {
            Parks.Add(park);
        }
    }
}
