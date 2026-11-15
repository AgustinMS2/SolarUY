using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

/// <summary>
/// Acceso a la historia por minuto de los inversores (spec 036).
/// </summary>
public interface IInverterMinuteReadingRepository
{
    /// <summary>
    /// Guarda solo los minutos que todavía no existen (clave inversor + minuto). Es idempotente: guardar dos veces lo mismo,
    /// o que otra copia del servidor guarde el mismo minuto a la vez, no duplica ni da error.
    /// </summary>
    void AddMissing(IReadOnlyCollection<InverterMinuteReading> readings);

    /// <summary>Último minuto guardado de cada inversor que tiene historia.</summary>
    IReadOnlyDictionary<Guid, DateTime> GetLastMinuteByInverter();

    /// <summary>Registros de un parque entre dos instantes UTC (el límite inferior incluido y el superior excluido).</summary>
    IReadOnlyList<InverterMinuteReading> GetByPark(Guid parkId, DateTime fromUtc, DateTime toUtc);

    int Count();
}
