using SolarUY.ApiService.Repositories;

namespace SolarUY.Tests.Fakes;

public class FakeHistoryCatchUpLock : IHistoryCatchUpLock
{
    /// <summary>Si es falso, simula que otra copia del servidor tiene el candado.</summary>
    public bool Available { get; set; } = true;

    public int Acquisitions { get; private set; }

    public int Releases { get; private set; }

    public IDisposable? TryAcquire()
    {
        if (!Available)
        {
            return null;
        }

        Acquisitions++;
        return new Releaser(this);
    }

    private sealed class Releaser(FakeHistoryCatchUpLock owner) : IDisposable
    {
        public void Dispose() => owner.Releases++;
    }
}
