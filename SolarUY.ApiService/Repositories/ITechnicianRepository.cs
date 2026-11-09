using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public interface ITacticianRepository
{
    bool ExistsByDocument(string documentId);

    bool ExistsByEmail(string email);

    void Add(Technician technician);

    IReadOnlyList<Technician> GetAll(TechnicianStatus? status = null);
}