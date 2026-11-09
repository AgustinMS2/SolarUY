using Microsoft.EntityFrameworkCore;
using SolarUY.ApiService.Data;
using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public class TacticianRepository : ITacticianRepository
{
    private readonly SolarUyDbContext _context;

    public TacticianRepository(SolarUyDbContext context)
    {
        _context = context;
    }

    public bool ExistsByDocument(string documentId)
    {
        return _context.Technicians.Any(t => t.NormalizedDocumentId == documentId);
    }

    public bool ExistsByEmail(string email)
    {
        return _context.Technicians.Any(t => t.NormalizedEmail == email);
    }

    public void Add(Technician technician)
    {
        _context.Technicians.Add(technician);
        _context.SaveChanges();
    }

    public IReadOnlyList<Technician> GetAll(TechnicianStatus? status = null)
    {
        var query = _context.Technicians.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        return query
            .OrderBy(t => t.FullName)
            .ToList()
            .AsReadOnly();
    }

    public Technician? GetById(Guid id)
    {
        return _context.Technicians.FirstOrDefault(t => t.Id == id);
    }

    public void Update(Technician technician)
    {
        _context.Technicians.Update(technician);
        _context.SaveChanges();
    }
}
