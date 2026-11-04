using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public interface ISolarNodeRepository
{
    bool ExistsByName(Guid parkId, string normalizedName);
    void Add(SolarNode node);
    SolarNode? GetById(Guid id);
}