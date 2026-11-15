namespace SolarUY.Tests.Fakes;

/// <summary>Reloj de pruebas: la hora solo cambia cuando el test la mueve.</summary>
public class TestTimeProvider : TimeProvider
{
    public DateTimeOffset Now { get; set; }

    public TestTimeProvider(DateTimeOffset start)
    {
        Now = start;
    }

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan time) => Now = Now.Add(time);
}
