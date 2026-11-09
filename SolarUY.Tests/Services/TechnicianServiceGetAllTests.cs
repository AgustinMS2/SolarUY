using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class TechnicianServiceGetAllTests
{
    [Fact]
    public void GetAll_WithNoStatusFilter_ReturnsAllTechniciansOrdered()
    {
        // Arrange
        var techRepo = new FakeTechnicianRepository();
        techRepo.PersistedTechnicians.Add(new Technician
        {
            Id = Guid.NewGuid(),
            FullName = "Zapata, Ana",
            DocumentId = "Z-001",
            NormalizedDocumentId = "zapataana",
            Email = "ana.zapata@email.com",
            NormalizedEmail = "ana.zapata@email.com",
            PhoneNumber = "+5491112345678",
            Status = TechnicianStatus.Active,
            CreatedAtUtc = DateTime.UtcNow,
            Specialty = "Electricidad"
        });
        techRepo.PersistedTechnicians.Add(new Technician
        {
            Id = Guid.NewGuid(),
            FullName = "Martinez, Carlos",
            DocumentId = "M-002",
            NormalizedDocumentId = "martinezcarlos",
            Email = "carlos.martinez@email.com",
            NormalizedEmail = "carlos.martinez@email.com",
            PhoneNumber = "+5491112345678",
            Status = TechnicianStatus.Active,
            CreatedAtUtc = DateTime.UtcNow,
            Specialty = "Fontanería"
        });
        techRepo.PersistedTechnicians.Add(new Technician
        {
            Id = Guid.NewGuid(),
            FullName = "Gomez, Beatriz",
            DocumentId = "G-003",
            NormalizedDocumentId = "gomezbea",
            Email = "beatriz.gomez@email.com",
            NormalizedEmail = "beatriz.gomez@email.com",
            PhoneNumber = "+5491112345678",
            Status = TechnicianStatus.Inactive,
            CreatedAtUtc = DateTime.UtcNow,
            Specialty = "Energía Solar"
        });
        var service = new TechnicianService(techRepo, NullLogger<TechnicianService>.Instance);

        // Act
        var result = service.GetAll();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.Value!.Count);
        // Verificar orden alfabético: Gomez, Martinez, Zapata
        Assert.Equal("Gomez, Beatriz", result.Value![0].FullName);
        Assert.Equal("Martinez, Carlos", result.Value![1].FullName);
        Assert.Equal("Zapata, Ana", result.Value![2].FullName);
    }

    [Fact]
    public void GetAll_WithActiveStatusFilter_ReturnsOnlyActiveTechnicians()
    {
        // Arrange
        var techRepo = new FakeTechnicianRepository();
        techRepo.PersistedTechnicians.Add(new Technician
        {
            Id = Guid.NewGuid(),
            FullName = "Activo, Tecnico",
            DocumentId = "A-001",
            NormalizedDocumentId = "activo tecnico",
            Email = "activo@email.com",
            NormalizedEmail = "activo@email.com",
            PhoneNumber = "+5491112345678",
            Status = TechnicianStatus.Active,
            CreatedAtUtc = DateTime.UtcNow,
            Specialty = "Electricidad"
        });
        techRepo.PersistedTechnicians.Add(new Technician
        {
            Id = Guid.NewGuid(),
            FullName = "Inactivo, Tecnico",
            DocumentId = "I-002",
            NormalizedDocumentId = "inactivo tecnico",
            Email = "inactivo@email.com",
            NormalizedEmail = "inactivo@email.com",
            PhoneNumber = "+5491112345678",
            Status = TechnicianStatus.Inactive,
            CreatedAtUtc = DateTime.UtcNow,
            Specialty = "Fontanería"
        });
        var service = new TechnicianService(techRepo, NullLogger<TechnicianService>.Instance);

        // Act
        var result = service.GetAll(status: TechnicianStatus.Active);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Value!);
        Assert.Equal("Activo, Tecnico", result.Value![0].FullName);
        Assert.Equal(TechnicianStatus.Active, result.Value![0].Status);
    }

    [Fact]
    public void GetAll_WithInactiveStatusFilter_ReturnsOnlyInactiveTechnicians()
    {
        // Arrange
        var techRepo = new FakeTechnicianRepository();
        techRepo.PersistedTechnicians.Add(new Technician
        {
            Id = Guid.NewGuid(),
            FullName = "Activo, Tecnico",
            DocumentId = "A-001",
            NormalizedDocumentId = "activo tecnico",
            Email = "activo@email.com",
            NormalizedEmail = "activo@email.com",
            PhoneNumber = "+5491112345678",
            Status = TechnicianStatus.Active,
            CreatedAtUtc = DateTime.UtcNow,
            Specialty = "Electricidad"
        });
        techRepo.PersistedTechnicians.Add(new Technician
        {
            Id = Guid.NewGuid(),
            FullName = "Inactivo, Tecnico",
            DocumentId = "I-002",
            NormalizedDocumentId = "inactivo tecnico",
            Email = "inactivo@email.com",
            NormalizedEmail = "inactivo@email.com",
            PhoneNumber = "+5491112345678",
            Status = TechnicianStatus.Inactive,
            CreatedAtUtc = DateTime.UtcNow,
            Specialty = "Fontanería"
        });
        var service = new TechnicianService(techRepo, NullLogger<TechnicianService>.Instance);

        // Act
        var result = service.GetAll(status: TechnicianStatus.Inactive);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Value!);
        Assert.Equal("Inactivo, Tecnico", result.Value![0].FullName);
        Assert.Equal(TechnicianStatus.Inactive, result.Value![0].Status);
    }
}