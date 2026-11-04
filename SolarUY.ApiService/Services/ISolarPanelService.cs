using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

public interface ISolarPanelService
{
    ServiceResult<Guid> Create(CreateSolarPanelRequest request);
    IReadOnlyList<SolarPanelDto> GetAll();
    ServiceResult<SolarPanelDto> GetById(Guid id);
    ServiceResult Update(Guid id, UpdateSolarPanelRequest request);
    ServiceResult DeletePanel(Guid id);
    ServiceResult AssignPanelsToNode(Guid nodeId, AssignPanelsRequest request);
    ServiceResult UnassignPanelsFromNode(Guid nodeId, UnassignPanelsRequest request);
}
