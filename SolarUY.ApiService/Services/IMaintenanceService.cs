using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

public interface IMaintenanceService
{
    ServiceResult<Guid> CreateReservation(CreateMaintenanceReservationRequest request);
}
