using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

public interface ISolarPanelService
{
    ServiceResult<Guid> Create(CreateSolarPanelRequest request);
    IReadOnlyList<SolarPanelDto> GetAll();
}
