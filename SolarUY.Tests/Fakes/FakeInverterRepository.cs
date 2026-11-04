using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Repositories;

namespace SolarUY.Tests.Fakes;

public class FakeInverterRepository : IInverterRepository
{
    public List<Inverter> Inverters { get; } = [];

    public Inverter? GetBySerie(string normalizedSerie)
    {
        return Inverters.FirstOrDefault(i => i.NormalizedSerieNumber == normalizedSerie);
    }

    public Inverter? GetByNodeId(Guid nodeId)
    {
        return Inverters.FirstOrDefault(i => i.SolarNodeId == nodeId);
    }

    public IReadOnlyList<Inverter> GetByNodeIds(IReadOnlyCollection<Guid> nodeIds)
    {
        return Inverters.Where(i => nodeIds.Contains(i.SolarNodeId)).ToList();
    }

    public void Add(Inverter inverter)
    {
        Inverters.Add(inverter);
    }

    public void Update(Inverter inverter)
    {
        var index = Inverters.FindIndex(i => i.Id == inverter.Id);
        if (index >= 0)
        {
            Inverters[index] = inverter;
        }
    }

    public void Remove(Inverter inverter)
    {
        Inverters.RemoveAll(i => i.Id == inverter.Id);
    }
}
