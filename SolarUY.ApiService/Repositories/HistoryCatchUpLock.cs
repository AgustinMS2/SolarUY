using Microsoft.EntityFrameworkCore;
using SolarUY.ApiService.Data;

namespace SolarUY.ApiService.Repositories;

/// <summary>Candado de sesión de PostgreSQL (<c>pg_try_advisory_lock</c>): si la copia se cae, la base lo libera sola.</summary>
public class HistoryCatchUpLock : IHistoryCatchUpLock
{
    // Clave arbitraria y fija para toda la aplicación ("036" de la spec).
    private const long LockKey = 3600001;

    private readonly SolarUyDbContext _context;

    public HistoryCatchUpLock(SolarUyDbContext context)
    {
        _context = context;
    }

    public IDisposable? TryAcquire()
    {
        // Se abre la conexión a mano: el candado pertenece a la sesión y debe liberarse en la misma conexión.
        _context.Database.OpenConnection();
        try
        {
            using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"SELECT pg_try_advisory_lock({LockKey})";
            if (command.ExecuteScalar() is true)
            {
                return new Releaser(_context);
            }
        }
        catch
        {
            _context.Database.CloseConnection();
            throw;
        }

        _context.Database.CloseConnection();
        return null;
    }

    private sealed class Releaser(SolarUyDbContext context) : IDisposable
    {
        public void Dispose()
        {
            try
            {
                using var command = context.Database.GetDbConnection().CreateCommand();
                command.CommandText = $"SELECT pg_advisory_unlock({LockKey})";
                command.ExecuteScalar();
            }
            finally
            {
                context.Database.CloseConnection();
            }
        }
    }
}
