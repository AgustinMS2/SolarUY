using Microsoft.Extensions.Logging;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Repositories;

namespace SolarUY.ApiService.Services;

public interface IMaintenanceService
{
    ServiceResult<Guid> CreateReservation(CreateMaintenanceReservationRequest request);

    ServiceResult<IReadOnlyList<MaintenanceOrderDto>> GetAll(MaintenanceFilter filter);

    ServiceResult<MaintenanceOrderDto> GetById(Guid orderId);

    ServiceResult UpdateReservation(Guid orderId, UpdateMaintenanceReservationRequest request);

    ServiceResult CancelReservation(Guid orderId, CancelMaintenanceRequest request);
}
