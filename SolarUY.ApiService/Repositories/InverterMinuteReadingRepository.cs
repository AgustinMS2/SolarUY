using Microsoft.EntityFrameworkCore;
using SolarUY.ApiService.Data;
using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public class InverterMinuteReadingRepository : IInverterMinuteReadingRepository
{
    // Reintentos del lote completo cuando otra copia del servidor guarda los mismos registros a la vez.
    private const int MaxBatchAttempts = 4;

    private readonly SolarUyDbContext _context;

    public InverterMinuteReadingRepository(SolarUyDbContext context)
    {
        _context = context;
    }

    public void AddMissing(IReadOnlyCollection<InverterMinuteReading> readings)
    {
        if (readings.Count == 0)
        {
            return;
        }

        // Npgsql solo acepta fechas con Kind UTC para timestamptz.
        foreach (var reading in readings)
        {
            reading.MinuteUtc = DateTime.SpecifyKind(reading.MinuteUtc, DateTimeKind.Utc);
        }

        var inverterIds = readings.Select(r => r.InverterId).Distinct().ToList();
        var from = readings.Min(r => r.MinuteUtc);
        var to = readings.Max(r => r.MinuteUtc);
        var pending = readings.ToList();

        for (var attempt = 1; attempt <= MaxBatchAttempts && pending.Count > 0; attempt++)
        {
            var existing = _context.InverterMinuteReadings
                .AsNoTracking()
                .Where(r => inverterIds.Contains(r.InverterId) && r.MinuteUtc >= from && r.MinuteUtc <= to)
                .Select(r => new { r.InverterId, r.MinuteUtc })
                .ToList()
                .Select(r => (r.InverterId, r.MinuteUtc))
                .ToHashSet();

            pending = pending.Where(r => !existing.Contains((r.InverterId, r.MinuteUtc))).ToList();
            if (pending.Count == 0)
            {
                return;
            }

            try
            {
                _context.InverterMinuteReadings.AddRange(pending);
                _context.SaveChanges();
                return;
            }
            catch (DbUpdateException)
            {
                // Otra copia del servidor guardó alguno de estos minutos entre la consulta y el guardado (clave duplicada):
                // se vuelve a consultar qué existe y se reintenta solo con lo que falta.
                _context.ChangeTracker.Clear();
            }
        }

        // Última defensa, poco frecuente: de a uno, ignorando los que otra copia ya guardó, para no dejar huecos.
        foreach (var reading in pending)
        {
            try
            {
                _context.InverterMinuteReadings.Add(reading);
                _context.SaveChanges();
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
            }
        }
    }

    public IReadOnlyDictionary<Guid, DateTime> GetLastMinuteByInverter()
    {
        return _context.InverterMinuteReadings
            .AsNoTracking()
            .GroupBy(r => r.InverterId)
            .Select(g => new { InverterId = g.Key, LastMinute = g.Max(r => r.MinuteUtc) })
            .ToList()
            .ToDictionary(x => x.InverterId, x => DateTime.SpecifyKind(x.LastMinute, DateTimeKind.Utc));
    }

    public IReadOnlyList<InverterMinuteReading> GetByPark(Guid parkId, DateTime fromUtc, DateTime toUtc)
    {
        var from = DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(toUtc, DateTimeKind.Utc);

        return _context.InverterMinuteReadings
            .AsNoTracking()
            .Where(r => r.SolarParkId == parkId && r.MinuteUtc >= from && r.MinuteUtc < to)
            .OrderBy(r => r.MinuteUtc)
            .ToList();
    }

    public int Count()
    {
        return _context.InverterMinuteReadings.Count();
    }
}
