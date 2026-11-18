namespace SolarUY.Web;

/// <summary>Dirección lógica del API para Aspire service discovery.</summary>
public static class ApiServiceEndpoint
{
    // "apiservice" no es un host real: el service discovery de Aspire lo reemplaza por la dirección
    // verdadera del API (https cuando está disponible), así que no viaja tráfico en claro por este nombre.
#pragma warning disable S5332 // Clear-text protocols should not be used (nombre lógico, no una URL real)
    public const string BaseAddress = "http://apiservice";
#pragma warning restore S5332
}
