using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;

namespace SolarUY.ApiService.Services;

public interface ISolarNodeService
{
    ServiceResult<Guid> Create(CreateSolarNodeRequest request);
    IReadOnlyList<SolarNodeDto> GetAll(Guid? parkId = null);
    ServiceResult<SolarNodeDto> GetById(Guid id);
    ServiceResult Update(Guid id, UpdateSolarNodeRequest request);
    ServiceResult AssignInverter(Guid nodeId, AssignInverterRequest request);
    ServiceResult UnassignInverter(Guid nodeId);
}
