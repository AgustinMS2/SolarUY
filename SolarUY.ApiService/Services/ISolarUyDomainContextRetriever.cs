using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Services;

public record DomainParkInfo(Guid Id, string Name, double CapacityKw, string Status);
public record DomainAlertInfo(int PendingCount, int AcknowledgedCount, IReadOnlyList<string> RecentAlertMessages);
public record DomainMaintenanceInfo(int PendingCount, IReadOnlyList<string> UpcomingOrders);
public record DomainGenerationInfo(Guid ParkId, string ParkName, double TodayKwh, double MonthKwh);

public record SolarUyDomainSnapshot(
    IReadOnlyList<DomainParkInfo> Parks,
    DomainAlertInfo Alerts,
    DomainMaintenanceInfo Maintenances,
    IReadOnlyList<DomainGenerationInfo> Generations
);

public interface ISolarUyDomainContextRetriever
{
    SolarUyDomainSnapshot GetCurrentSnapshot();
}

