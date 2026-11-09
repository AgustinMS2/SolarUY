using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Repositories;

namespace SolarUY.Tests.Fakes;

public class FakeTechnicianRepository : ITacticianRepository
{
    public List<Technician> PersistedTechnicians { get; } = [];

    public bool ExistsByDocument(string documentId)
    {
        return PersistedTechnicians.Any(t => t.DocumentId == documentId);
    }

    public bool ExistsByEmail(string email)
    {
        return PersistedTechnicians.Any(t => t.Email == email);
    }

    public void Add(Technician technician)
    {
        PersistedTechnicians.Add(technician);
    }

    public IReadOnlyList<Technician> GetAll(TechnicianStatus? status = null)
    {
        var query = PersistedTechnicians.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == (TechnicianStatus)status.Value);
        }

        return query
            .OrderBy(t => t.FullName)
            .ToList()
            .AsReadOnly();
    }
}