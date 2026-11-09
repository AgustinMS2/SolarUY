using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

public interface ITechnicianService
{
    ServiceResult<Guid> Create(CreateTechnicianRequest request);
    ServiceResult<IReadOnlyList<TechnicianDto>> GetAll(TechnicianStatus? status = null);
}