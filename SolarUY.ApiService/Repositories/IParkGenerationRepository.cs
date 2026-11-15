using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public interface IParkGenerationRepository
{
    IReadOnlyList<ParkHourlyGeneration> GetByParkAndDateRange(Guid parkId, DateOnly fromDate, DateOnly toDate);
    void AddRange(IEnumerable<ParkHourlyGeneration> records);
    bool HasAnyForPark(Guid parkId);

    /// <summary>
    /// Guarda solo las horas que todavía no existen (clave parque + fecha local + hora local). Es idempotente: guardar dos
    /// veces lo mismo, o que otra copia del servidor guarde la misma hora a la vez, no duplica ni da error (spec 036).
    /// </summary>
    void AddMissing(IReadOnlyCollection<ParkHourlyGeneration> records);

    /// <summary>Inicio (UTC) de la última hora guardada del parque, o <c>null</c> si no tiene historia (spec 036).</summary>
    DateTime? GetLastHourByPark(Guid parkId);
}
