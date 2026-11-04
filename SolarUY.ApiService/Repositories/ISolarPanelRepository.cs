using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public interface ISolarPanelRepository
{
    bool ExistsBySerieNumber(string normalizedSerieNumber);
    void Add(SolarPanel panel);
    SolarPark? GetParkById(Guid parkId);
    IReadOnlyList<SolarPanel> GetAll();
}
