namespace SolarUY.ApiService.Services;

/// <summary>
/// Simula la lectura instantánea de un panel a partir de lo esperado a esa hora (Spec 030).
/// Misma fórmula que <c>PanelTelemetryBackgroundService</c> de la spec 029: variación leve (±3 %) y,
/// en ~5 % de las lecturas, una caída fuerte (sombreado o suciedad). Se duplica a propósito para no
/// tocar la spec 029; se unificará con el simulador de la spec 036.
/// </summary>
public static class PanelReadingSimulator
{
    public static double SimulateActivePowerW(double expectedPowerW)
    {
        if (expectedPowerW <= 0.0)
        {
            return 0.0;
        }

        var factor = Random.Shared.NextDouble() < 0.05
            ? 0.20 + (Random.Shared.NextDouble() * 0.15)
            : 0.97 + (Random.Shared.NextDouble() * 0.06);

        return Math.Round(expectedPowerW * factor, 1);
    }
}
