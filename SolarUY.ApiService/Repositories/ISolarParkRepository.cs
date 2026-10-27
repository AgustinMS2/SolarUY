using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public interface ISolarParkRepository
{
    bool ExistsByNormalizedName(string normalizedName);
    bool ExistsByNormalizedName(string normalizedName, Guid excludeId);
    void Add(SolarPark park);
    IReadOnlyList<SolarPark> GetAll();
    SolarPark? GetById(Guid id);
    void Update(SolarPark park);
}
