using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public interface IInverterRepository
{
    Inverter? GetById(Guid id);
    Inverter? GetBySerie(string normalizedSerie);
    Inverter? GetByNodeId(Guid nodeId);
    IReadOnlyList<Inverter> GetByNodeIds(IReadOnlyCollection<Guid> nodeIds);
    void Add(Inverter inverter);
    void Update(Inverter inverter);
    void Remove(Inverter inverter);
}
