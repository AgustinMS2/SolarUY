using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

public interface ISolarParkService
{
    ServiceResult<Guid> Create(CreateSolarParkRequest request);
}
