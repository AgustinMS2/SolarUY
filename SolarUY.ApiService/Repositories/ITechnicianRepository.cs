using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public interface ITechnicianRepository
{
    bool ExistsByDocument(string documentId);

    bool ExistsByEmail(string email);

    void Add(Technician technician);

    IReadOnlyList<Technician> GetAll(TechnicianStatus? status = null);

    Technician? GetById(Guid id);

    void Update(Technician technician);
}