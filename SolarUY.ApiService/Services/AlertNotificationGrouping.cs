using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

/// <summary>Una alerta que llegó por SignalR, con el momento en que llegó.</summary>
public record AlertArrival(AlertDto Alert, DateTime ArrivedAtUtc);

/// <summary>Aviso (toast) a mostrar: una alerta suelta o una ráfaga agrupada de un parque.</summary>
public record AlertToast(Guid? AlertId, Guid ParkId, string ParkName, AlertSeverity Severity, string Message, int Count);

/// <summary>
/// Regla de la ráfaga de avisos (Spec 031, RF-8): si llegan 5 o más alertas distintas del mismo parque
/// dentro de 10 segundos, se muestra un único aviso "Se detectaron N fallas simultáneas en &lt;Parque&gt;"
/// en lugar de un aviso por alerta, para no saturar la pantalla. Las alertas siguen registradas una por una.
/// </summary>
public static class AlertNotificationGrouping
{
    public const int BurstThreshold = 5;
    public static readonly TimeSpan BurstWindow = TimeSpan.FromSeconds(10);

    public static IReadOnlyList<AlertToast> Group(IEnumerable<AlertArrival> arrivals, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(arrivals);

        // Solo cuentan las llegadas de los últimos 10 s; una alerta que llegó dos veces (por consolidación) cuenta una vez.
        var recent = arrivals
            .Where(a => nowUtc - a.ArrivedAtUtc <= BurstWindow)
            .GroupBy(a => a.Alert.Id)
            .Select(g => new
            {
                Alert = g.OrderByDescending(a => a.ArrivedAtUtc).First().Alert,
                FirstArrivedAtUtc = g.Min(a => a.ArrivedAtUtc)
            })
            .OrderBy(a => a.FirstArrivedAtUtc)
            .ToList();

        var burstParks = recent
            .GroupBy(a => a.Alert.ParkId)
            .Where(g => g.Count() >= BurstThreshold)
            .Select(g => g.Key)
            .ToHashSet();

        var toasts = new List<AlertToast>();
        var burstsAlreadyAdded = new HashSet<Guid>();

        foreach (var item in recent)
        {
            var alert = item.Alert;

            if (!burstParks.Contains(alert.ParkId))
            {
                toasts.Add(new AlertToast(alert.Id, alert.ParkId, alert.ParkName, alert.Severity, $"{alert.EquipmentName}: {alert.Description}", 1));
                continue;
            }

            // El aviso agrupado ocupa el lugar de la primera alerta de la ráfaga.
            if (!burstsAlreadyAdded.Add(alert.ParkId))
            {
                continue;
            }

            var parkAlerts = recent.Where(a => a.Alert.ParkId == alert.ParkId).Select(a => a.Alert).ToList();
            toasts.Add(new AlertToast(
                null,
                alert.ParkId,
                alert.ParkName,
                parkAlerts.Max(a => a.Severity),
                $"Se detectaron {parkAlerts.Count} fallas simultáneas en {alert.ParkName}",
                parkAlerts.Count));
        }

        return toasts;
    }
}
