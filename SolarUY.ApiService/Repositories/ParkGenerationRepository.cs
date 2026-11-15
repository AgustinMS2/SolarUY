using Microsoft.EntityFrameworkCore;
using SolarUY.ApiService.Data;
using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public class ParkGenerationRepository : IParkGenerationRepository
{
    // Reintentos del lote completo cuando otra copia del servidor guarda los mismos registros a la vez.
    private const int MaxBatchAttempts = 4;

    private readonly SolarUyDbContext _context;

    public ParkGenerationRepository(SolarUyDbContext context)
    {
        _context = context;
    }

    public IReadOnlyList<ParkHourlyGeneration> GetByParkAndDateRange(Guid parkId, DateOnly fromDate, DateOnly toDate)
    {
        return _context.ParkHourlyGenerations
            .Where(g => g.SolarParkId == parkId && g.DateLocal >= fromDate && g.DateLocal <= toDate)
            .OrderBy(g => g.DateLocal)
            .ThenBy(g => g.HourLocal)
            .ToList();
    }

    public void AddRange(IEnumerable<ParkHourlyGeneration> records)
    {
        _context.ParkHourlyGenerations.AddRange(records);
        _context.SaveChanges();
    }

    public bool HasAnyForPark(Guid parkId)
    {
        return _context.ParkHourlyGenerations.Any(g => g.SolarParkId == parkId);
    }

    public void AddMissing(IReadOnlyCollection<ParkHourlyGeneration> records)
    {
        if (records.Count == 0)
        {
            return;
        }

        // Npgsql solo acepta fechas con Kind UTC para timestamptz.
        foreach (var record in records)
        {
            record.TimestampUtc = DateTime.SpecifyKind(record.TimestampUtc, DateTimeKind.Utc);
        }

        var parkIds = records.Select(r => r.SolarParkId).Distinct().ToList();
        var from = records.Min(r => r.DateLocal);
        var to = records.Max(r => r.DateLocal);
        var pending = records.ToList();

        for (var attempt = 1; attempt <= MaxBatchAttempts && pending.Count > 0; attempt++)
        {
            var existing = _context.ParkHourlyGenerations
                .AsNoTracking()
                .Where(g => parkIds.Contains(g.SolarParkId) && g.DateLocal >= from && g.DateLocal <= to)
                .Select(g => new { g.SolarParkId, g.DateLocal, g.HourLocal })
                .ToList()
                .Select(g => (g.SolarParkId, g.DateLocal, g.HourLocal))
                .ToHashSet();

            pending = pending.Where(r => !existing.Contains((r.SolarParkId, r.DateLocal, r.HourLocal))).ToList();
            if (pending.Count == 0)
            {
                return;
            }

            try
            {
                _context.ParkHourlyGenerations.AddRange(pending);
                _context.SaveChanges();
                return;
            }
            catch (DbUpdateException)
            {
                // Otra copia del servidor guardó alguna de estas horas entre la consulta y el guardado (clave única duplicada):
                // se vuelve a consultar qué existe y se reintenta solo con lo que falta.
                _context.ChangeTracker.Clear();
            }
        }

        // Última defensa, poco frecuente: de a una, ignorando las que otra copia ya guardó, para no dejar huecos.
        foreach (var record in pending)
        {
            try
            {
                _context.ParkHourlyGenerations.Add(record);
                _context.SaveChanges();
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
            }
        }
    }

    public DateTime? GetLastHourByPark(Guid parkId)
    {
        var last = _context.ParkHourlyGenerations
            .AsNoTracking()
            .Where(g => g.SolarParkId == parkId)
            .OrderByDescending(g => g.TimestampUtc)
            .Select(g => (DateTime?)g.TimestampUtc)
            .FirstOrDefault();

        return last.HasValue ? DateTime.SpecifyKind(last.Value, DateTimeKind.Utc) : null;
    }
}
