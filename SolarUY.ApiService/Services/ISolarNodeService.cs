using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;

namespace SolarUY.ApiService.Services;

public interface ISolarNodeService
{
    ServiceResult<Guid> Create(CreateSolarNodeRequest request);
}