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

    public void Add(SolarPark park)
    {
        Parks.Add(park);
    }
}
