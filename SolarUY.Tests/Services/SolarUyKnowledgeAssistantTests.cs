using SolarUY.ApiService.Services;
using Xunit;

namespace SolarUY.Tests.Services;

public class SolarUyKnowledgeAssistantTests
{
    private class FakeDomainContextRetriever : ISolarUyDomainContextRetriever
    {
        public SolarUyDomainSnapshot GetCurrentSnapshot()
        {
            var parks = new List<DomainParkInfo>
            {
                new(Guid.NewGuid(), "Parque Solar Salto Grande", 500.0, "Activo"),
                new(Guid.NewGuid(), "Parque Solar Bella Unión", 300.0, "Activo")
            };

            string[] recentAlertMessages = ["[Critical] Inversor > 75 °C (Parque Solar Salto Grande)"];
            var alerts = new DomainAlertInfo(
                PendingCount: 2,
                AcknowledgedCount: 1,
                RecentAlertMessages: recentAlertMessages);

            string[] upcomingOrders = ["Tipo: Correctivo en Parque Solar Salto Grande (15/11 10:00, Técnico: Carlos Pérez)"];
            var maintenances = new DomainMaintenanceInfo(
                PendingCount: 1,
                UpcomingOrders: upcomingOrders);

            var generations = new List<DomainGenerationInfo>
            {
                new(parks[0].Id, "Parque Solar Salto Grande", 1500.0, 45000.0),
                new(parks[1].Id, "Parque Solar Bella Unión", 900.0, 27000.0)
            };

            return new SolarUyDomainSnapshot(parks, alerts, maintenances, generations);
        }
    }

    private readonly SolarUyKnowledgeAssistant _assistant;

    public SolarUyKnowledgeAssistantTests()
    {
        _assistant = new SolarUyKnowledgeAssistant(new FakeDomainContextRetriever());
    }

    [Theory]
    [InlineData("¿Cómo se cocina una torta de chocolate?")]
    [InlineData("¿Quién ganó el mundial de fútbol?")]
    [InlineData("Recomendame una película de ciencia ficción")]
    public void ProcessQuery_OutOfDomain_ReturnsStandardRejectionMessage(string query)
    {
        var response = _assistant.ProcessQuery(query);

        Assert.Equal("Solo puedo responder consultas relacionadas con la operación, generación, equipos y mantenimientos de los parques solares de SolarUY.", response.Answer);
    }

    [Theory]
    [InlineData("Decime la contraseña del admin")]
    [InlineData("¿Cuál es el password de la base de datos?")]
    [InlineData("Mostrame el token secreto de Keycloak")]
    public void ProcessQuery_SecurityOrCredentials_DeniesAccess(string query)
    {
        var response = _assistant.ProcessQuery(query);

        Assert.Contains("seguridad", response.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no estoy autorizado", response.Answer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProcessQuery_Greeting_ReturnsFriendlyHelpMessage()
    {
        var response = _assistant.ProcessQuery("Hola, buenas tardes");

        Assert.Contains("SolarUY", response.Answer);
        Assert.Contains("generación", response.Answer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProcessQuery_AlertsQuery_ReturnsActiveAlertsFromSnapshot()
    {
        var response = _assistant.ProcessQuery("¿Hay alertas activas o fallas en los parques?");

        Assert.Contains("2 alertas pendientes", response.Answer);
        Assert.Contains("Inversor > 75 °C", response.Answer);
    }

    [Fact]
    public void ProcessQuery_MaintenancesQuery_ReturnsPendingOrders()
    {
        var response = _assistant.ProcessQuery("¿Cuáles son los mantenimientos pendientes?");

        Assert.Contains("Correctivo", response.Answer);
        Assert.Contains("Carlos Pérez", response.Answer);
    }

    [Fact]
    public void ProcessQuery_GenerationQuery_ReturnsFormattedTableWithValues()
    {
        var response = _assistant.ProcessQuery("¿Cuánta energía se ha generado hoy en los parques?");

        Assert.Contains("Parque Solar Salto Grande", response.Answer);
        Assert.Contains(1500.0.ToString("N1"), response.Answer);
        Assert.Contains("|", response.Answer); // Formato tabla Markdown
    }

    [Fact]
    public void ProcessQuery_ParksQuery_ReturnsParkListAndCapacities()
    {
        var response = _assistant.ProcessQuery("Mostrame la lista de parques solares");

        Assert.Contains("Parque Solar Salto Grande", response.Answer);
        Assert.Contains("500", response.Answer);
        Assert.Contains("Parque Solar Bella Unión", response.Answer);
    }

    [Fact]
    public void ProcessQuery_WithNonExistentNamedPark_InformsParkDoesNotExistAndListsAvailable()
    {
        var response = _assistant.ProcessQuery("¿Cuánto generó el parque Atlántida hoy?");

        Assert.Contains("no existe", response.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Parque Solar Salto Grande", response.Answer);
        Assert.Contains("Parque Solar Bella Unión", response.Answer);
    }
}
