using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public interface ISolarNodeRepository
{
    bool ExistsByName(Guid parkId, string normalizedName);
    bool ExistsByName(Guid parkId, string normalizedName, Guid excludeId);
    void Add(SolarNode node);
    void Update(SolarNode node);
    SolarNode? GetById(Guid id);
    IReadOnlyList<SolarNode> GetAll(Guid? parkId = null);
}
