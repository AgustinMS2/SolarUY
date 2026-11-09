using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class TechnicianServiceCreateTests
{
    [Fact]
    public void Create_WithValidData_ReturnsSuccessAndPersistsTechnician()
    {
        // Arrange
        var techRepo = new FakeTechnicianRepository();
        var service = new TechnicianService(techRepo, logger: NullLogger<TechnicianService>.Instance);
        var request = new CreateTechnicianRequest
        {
            FullName = "Juan Pérez",
            DocumentId = "12345678-A",
            Email = "juan.perez@email.com",
            PhoneNumber = "+5491112345678"
        };

        // Act
        var result = service.Create(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.Value);
        Assert.Single(techRepo.PersistedTechnicians);

        var savedTech = techRepo.PersistedTechnicians[0];
        Assert.Equal(result.Value, savedTech.Id);
        Assert.Equal("Juan Pérez", savedTech.FullName);
        Assert.Equal("12345678a", savedTech.DocumentId);
        Assert.Equal("juan.perez@email.com", savedTech.Email);
        Assert.Equal("+5491112345678", savedTech.PhoneNumber);
        Assert.Equal(TechnicianStatus.Active, savedTech.Status);
        Assert.True(savedTech.CreatedAtUtc <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyFullName_ReturnsValidationError(string? fullName)
    {
        // Arrange
        var techRepo = new FakeTechnicianRepository();
        var service = new TechnicianService(techRepo, logger: NullLogger<TechnicianService>.Instance);
        var request = new CreateTechnicianRequest
        {
            FullName = fullName!,
            DocumentId = "12345678-A",
            Email = "juan.perez@email.com",
            PhoneNumber = "+5491112345678"
        };

        // Act
        var result = service.Create(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El nombre completo es obligatorio.", result.ErrorMessage);
        Assert.Empty(techRepo.PersistedTechnicians);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyDocumentId_ReturnsValidationError(string? documentId)
    {
        // Arrange
        var techRepo = new FakeTechnicianRepository();
        var service = new TechnicianService(techRepo, logger: NullLogger<TechnicianService>.Instance);
        var request = new CreateTechnicianRequest
        {
            FullName = "Juan Pérez",
            DocumentId = documentId!,
            Email = "juan.perez@email.com",
            PhoneNumber = "+5491112345678"
        };

        // Act
        var result = service.Create(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El documento de identidad es obligatorio.", result.ErrorMessage);
        Assert.Empty(techRepo.PersistedTechnicians);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyEmail_ReturnsValidationError(string? email)
    {
        // Arrange
        var techRepo = new FakeTechnicianRepository();
        var service = new TechnicianService(techRepo, logger: NullLogger<TechnicianService>.Instance);
        var request = new CreateTechnicianRequest
        {
            FullName = "Juan Pérez",
            DocumentId = "12345678-A",
            Email = email!,
            PhoneNumber = "+5491112345678"
        };

        // Act
        var result = service.Create(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El correo electrónico es obligatorio.", result.ErrorMessage);
        Assert.Empty(techRepo.PersistedTechnicians);
    }

    [Theory]
    [InlineData("usario@")]
    [InlineData("usuario")]
    public void Create_WithInvalidEmailFormat_ReturnsValidationError(string? email)
    {
        // Arrange
        var techRepo = new FakeTechnicianRepository();
        var service = new TechnicianService(techRepo, logger: NullLogger<TechnicianService>.Instance);
        var request = new CreateTechnicianRequest
        {
            FullName = "Juan Pérez",
            DocumentId = "12345678-A",
            Email = email!,
            PhoneNumber = "+5491112345678"
        };

        // Act
        var result = service.Create(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El formato del correo electrónico no es válido.", result.ErrorMessage);
        Assert.Empty(techRepo.PersistedTechnicians);
    }

    [Theory]
    [InlineData("12345678-A")]
    [InlineData("12.345.678-A")]
    public void Create_WithDuplicateDocument_ReturnsConflictError(string? documentId)
    {
        // Arrange - normalize like the service does: lowercase + remove dots/dashes
        var normalizedDoc = documentId!.ToLowerInvariant().Replace(".", "").Replace("-", "");
        var techRepo = new FakeTechnicianRepository();
        // Add technician with normalized document (service normalizes before checking)
        // The repository ExistsByDocument compares t.DocumentId against the normalized input
        // So we store the technician with the normalized DocumentId
        techRepo.PersistedTechnicians.Add(new Technician
        {
            Id = Guid.NewGuid(),
            FullName = "Técnico Existente",
            DocumentId = normalizedDoc, // e.g., "12345678a"
            NormalizedDocumentId = normalizedDoc, // e.g., "12345678a"
            Email = "otro@email.com",
            NormalizedEmail = "otro@email.com",
            PhoneNumber = "+5491112345678",
            Status = TechnicianStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        });
        var service = new TechnicianService(techRepo, logger: NullLogger<TechnicianService>.Instance);
        var request = new CreateTechnicianRequest
        {
            FullName = "Nuevo Técnico",
            DocumentId = documentId,
            Email = "nuevo@email.com",
            PhoneNumber = "+5491112345678"
        };

        // Act
        var result = service.Create(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Contains("Ya existe un técnico registrado con el documento", result.ErrorMessage!);
        // The service does not persist the technician when there's a conflict,
        // so the repository should still have the original technician from Arrange
        Assert.Single(techRepo.PersistedTechnicians);
    }

    [Theory]
    [InlineData("juan.perez@email.com")]
    [InlineData("OTRO@EMAIL.COM")]
    public void Create_WithDuplicateEmail_ReturnsConflictError(string? email)
    {
        // Arrange - normalize like the service does: lowercase
        var normalizedEmail = email!.ToLowerInvariant();
        var techRepo = new FakeTechnicianRepository();
        techRepo.PersistedTechnicians.Add(new Technician
        {
            Id = Guid.NewGuid(),
            FullName = "Técnico Existente",
            DocumentId = "12345678-A",
            NormalizedDocumentId = "12345678-a",
            Email = normalizedEmail,
            NormalizedEmail = normalizedEmail,
            PhoneNumber = "+5491112345678",
            Status = TechnicianStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        });
        var service = new TechnicianService(techRepo, logger: NullLogger<TechnicianService>.Instance);
        var request = new CreateTechnicianRequest
        {
            FullName = "Nuevo Técnico",
            DocumentId = "12345678-B",
            Email = email,
            PhoneNumber = "+5491112345678"
        };

        // Act
        var result = service.Create(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Contains("El correo electrónico", result.ErrorMessage!);
        // Service does not persist when there's a duplicate
        Assert.Single(techRepo.PersistedTechnicians);
    }

    [Theory]
    [InlineData("12345678-A")]
    [InlineData("12.345.678-A")]
    public void Create_WithPhoneTooShort_ReturnsValidationError(string? documentId)
    {
        // Arrange
        var techRepo = new FakeTechnicianRepository();
        var service = new TechnicianService(techRepo, logger: NullLogger<TechnicianService>.Instance);
        var request = new CreateTechnicianRequest
        {
            FullName = "Juan Pérez",
            DocumentId = documentId!.ToLowerInvariant(),
            Email = "juan.perez@email.com",
            PhoneNumber = "+123"  // Too short: only 3 digits after +
            ,
            Specialty = "Electricista"
        };

        // Act
        var result = service.Create(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El teléfono de contacto es obligatorio y debe tener un formato válido.", result.ErrorMessage!);
        Assert.Empty(techRepo.PersistedTechnicians);
    }

    [Theory]
    [InlineData("12345678-A")]
    [InlineData("12.345.678-A")]
    public void Create_WithPhoneTooLong_ReturnsValidationError(string? documentId)
    {
        // Arrange
        var techRepo = new FakeTechnicianRepository();
        var service = new TechnicianService(techRepo, logger: NullLogger<TechnicianService>.Instance);
        var phoneLong = "+" + new string('1', 21); // 21 digits after +
        var request = new CreateTechnicianRequest
        {
            FullName = "Juan Pérez",
            DocumentId = documentId!.ToLowerInvariant(),
            Email = "juan.perez@email.com",
            PhoneNumber = phoneLong,
            Specialty = "Electricista"
        };

        // Act
        var result = service.Create(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El teléfono de contacto es obligatorio y debe tener un formato válido.", result.ErrorMessage!);
        Assert.Empty(techRepo.PersistedTechnicians);
    }
}