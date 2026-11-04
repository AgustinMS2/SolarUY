using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public interface ISolarPanelRepository
{
    bool ExistsBySerieNumber(string normalizedSerieNumber);
    bool ExistsBySerieNumber(string normalizedSerieNumber, Guid excludeId);
    void Add(SolarPanel panel);
    SolarPark? GetParkById(Guid parkId);
    IReadOnlyList<SolarPanel> GetAll();
    SolarPanel? GetById(Guid id);
    void Update(SolarPanel panel);
}
