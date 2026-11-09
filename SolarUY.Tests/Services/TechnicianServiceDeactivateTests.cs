using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class TechnicianServiceDeactivateTests
{
    private static Technician CreateActiveTechnician(Guid id) => new()
    {
        Id = id,
        FullName = "Test, Técnico",
        DocumentId = "t-001",
        NormalizedDocumentId = "t001",
        Email = "tecnico.test@email.com",
        NormalizedEmail = "tecnico.test@email.com",
        PhoneNumber = "+5491112345678",
        Specialty = "Electricidad",
        Status = TechnicianStatus.Active,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static TechnicianService CreateService(
        FakeTechnicianRepository techRepo,
        FakeMaintenanceRepository maintenanceRepo)
    {
        return new TechnicianService(
            techRepo,
            maintenanceRepo,
            NullLogger<TechnicianService>.Instance);
    }

    [Fact]
    public void Deactivate_ActiveTechnicianWithoutPendingTasks_PassesToInactive()
    {
        // Arrange
        var techId = Guid.NewGuid();
        var techRepo = new FakeTechnicianRepository();
        techRepo.PersistedTechnicians.Add(CreateActiveTechnician(techId));
        var maintenanceRepo = new FakeMaintenanceRepository { PendingCount = 0 };
        var service = CreateService(techRepo, maintenanceRepo);

        // Act
        var result = service.Deactivate(techId);

        // Assert
        Assert.True(result.Success);
        var savedTech = techRepo.PersistedTechnicians.Single(t => t.Id == techId);
        Assert.Equal(TechnicianStatus.Inactive, savedTech.Status);
    }

    [Fact]
    public void Deactivate_TechnicianWithPendingTasks_ReturnsConflictAndDoesNotChangeStatus()
    {
        // Arrange
        var techId = Guid.NewGuid();
        var techRepo = new FakeTechnicianRepository();
        techRepo.PersistedTechnicians.Add(CreateActiveTechnician(techId));
        var maintenanceRepo = new FakeMaintenanceRepository { PendingCount = 2 };
        var service = CreateService(techRepo, maintenanceRepo);

        // Act
        var result = service.Deactivate(techId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "No se puede dar de baja al técnico porque tiene 2 mantenimiento(s) pendiente(s) de resolución. Reasigna o cancela esos mantenimientos previamente.",
            result.ErrorMessage);
        var savedTech = techRepo.PersistedTechnicians.Single(t => t.Id == techId);
        Assert.Equal(TechnicianStatus.Active, savedTech.Status);
    }

    [Fact]
    public void Deactivate_NonExistentTechnician_ReturnsNotFound()
    {
        // Arrange
        var techRepo = new FakeTechnicianRepository();
        var maintenanceRepo = new FakeMaintenanceRepository();
        var service = CreateService(techRepo, maintenanceRepo);

        // Act
        var result = service.Deactivate(Guid.NewGuid());

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("El técnico solicitado no existe.", result.ErrorMessage);
    }

    [Fact]
    public void Deactivate_AlreadyInactiveTechnician_ReturnsConflict()
    {
        // Arrange
        var techId = Guid.NewGuid();
        var techRepo = new FakeTechnicianRepository();
        techRepo.PersistedTechnicians.Add(CreateActiveTechnician(techId));
        techRepo.PersistedTechnicians.Single(t => t.Id == techId).Status = TechnicianStatus.Inactive;
        var maintenanceRepo = new FakeMaintenanceRepository();
        var service = CreateService(techRepo, maintenanceRepo);

        // Act
        var result = service.Deactivate(techId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal("El técnico ya está inactivo.", result.ErrorMessage);
    }
}
