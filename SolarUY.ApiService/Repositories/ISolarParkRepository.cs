using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public interface ISolarParkRepository
{
    bool ExistsByNormalizedName(string normalizedName);
    void Add(SolarPark park);
}
