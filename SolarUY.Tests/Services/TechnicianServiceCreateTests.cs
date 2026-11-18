using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Security;
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
        var keycloakRepo = new FakeKeycloakUserRepository();
        var service = new TechnicianService(techRepo, keycloakUserRepository: keycloakRepo, logger: NullLogger<TechnicianService>.Instance);
        var request = new CreateTechnicianRequest
        {
            FullName = "Juan Pérez",
            DocumentId = "12345678-A",
            Email = "juan.perez@email.com",
            PhoneNumber = "+5491112345678",
            Password = "Password123"
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

        // Spec 038 RF-1: también se crea la cuenta activa en Keycloak con rol Técnico y la contraseña indicada.
        Assert.Single(keycloakRepo.Users);
        var kcUser = keycloakRepo.Users[0];
        Assert.Equal("juan.perez@email.com", kcUser.Email);
        Assert.Equal("juan.perez@email.com", kcUser.Username);
        Assert.Equal("Juan Pérez", kcUser.FullName);
        Assert.True(kcUser.Enabled);
        Assert.Equal(Roles.Technician, kcUser.Role);
        Assert.Equal("Password123", keycloakRepo.PasswordsByEmail["juan.perez@email.com"]);
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
            PhoneNumber = "+5491112345678",
            Password = "Password123"
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
            PhoneNumber = "+5491112345678",
            Password = "Password123"
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
            PhoneNumber = "+5491112345678",
            Password = "Password123"
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
            PhoneNumber = "+5491112345678",
            Password = "Password123"
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
        // Arrange
        var normalizedDoc = documentId!.ToLowerInvariant().Replace(".", "").Replace("-", "");
        var techRepo = new FakeTechnicianRepository();
        techRepo.PersistedTechnicians.Add(new Technician
        {
            Id = Guid.NewGuid(),
            FullName = "Técnico Existente",
            DocumentId = normalizedDoc,
            NormalizedDocumentId = normalizedDoc,
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
            PhoneNumber = "+5491112345678",
            Password = "Password123"
        };

        // Act
        var result = service.Create(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Contains("Ya existe un técnico registrado con el documento", result.ErrorMessage!);
        Assert.Single(techRepo.PersistedTechnicians);
    }

    [Theory]
    [InlineData("juan.perez@email.com")]
    [InlineData("OTRO@EMAIL.COM")]
    public void Create_WithDuplicateEmail_ReturnsConflictError(string? email)
    {
        // Arrange
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
            PhoneNumber = "+5491112345678",
            Password = "Password123"
        };

        // Act
        var result = service.Create(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Contains("El correo electrónico", result.ErrorMessage!);
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
            PhoneNumber = "+123",
            Specialty = "Electricista",
            Password = "Password123"
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
        var phoneLong = "+" + new string('1', 21);
        var request = new CreateTechnicianRequest
        {
            FullName = "Juan Pérez",
            DocumentId = documentId!.ToLowerInvariant(),
            Email = "juan.perez@email.com",
            PhoneNumber = phoneLong,
            Specialty = "Electricista",
            Password = "Password123"
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
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    public void Create_WithEmptyOrShortPassword_ReturnsValidationError(string password)
    {
        // Arrange
        var techRepo = new FakeTechnicianRepository();
        var keycloakRepo = new FakeKeycloakUserRepository();
        var service = new TechnicianService(techRepo, keycloakUserRepository: keycloakRepo, logger: NullLogger<TechnicianService>.Instance);
        var request = new CreateTechnicianRequest
        {
            FullName = "Juan Pérez",
            DocumentId = "12345678-A",
            Email = "juan.perez@email.com",
            PhoneNumber = "+5491112345678",
            Password = password
        };

        // Act
        var result = service.Create(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("La contraseña es obligatoria y debe tener al menos 6 caracteres.", result.ErrorMessage);
        Assert.Empty(techRepo.PersistedTechnicians);
        Assert.Empty(keycloakRepo.Users);
    }

    [Fact]
    public void Create_WithPasswordTooLong_ReturnsValidationError()
    {
        // Arrange
        var techRepo = new FakeTechnicianRepository();
        var keycloakRepo = new FakeKeycloakUserRepository();
        var service = new TechnicianService(techRepo, keycloakUserRepository: keycloakRepo, logger: NullLogger<TechnicianService>.Instance);
        var request = new CreateTechnicianRequest
        {
            FullName = "Juan Pérez",
            DocumentId = "12345678-A",
            Email = "juan.perez@email.com",
            PhoneNumber = "+5491112345678",
            Password = new string('x', 101)
        };

        // Act
        var result = service.Create(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("La contraseña no puede superar los 100 caracteres.", result.ErrorMessage);
        Assert.Empty(techRepo.PersistedTechnicians);
        Assert.Empty(keycloakRepo.Users);
    }

    [Fact]
    public void Create_WithSpecialtyTooLong_ReturnsValidationError()
    {
        // Arrange
        var techRepo = new FakeTechnicianRepository();
        var keycloakRepo = new FakeKeycloakUserRepository();
        var service = new TechnicianService(techRepo, keycloakUserRepository: keycloakRepo, logger: NullLogger<TechnicianService>.Instance);
        var request = new CreateTechnicianRequest
        {
            FullName = "Juan Pérez",
            DocumentId = "12345678-A",
            Email = "juan.perez@email.com",
            PhoneNumber = "+5491112345678",
            Specialty = new string('s', 101),
            Password = "Password123"
        };

        // Act
        var result = service.Create(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("La especialidad no puede superar los 100 caracteres.", result.ErrorMessage);
        Assert.Empty(techRepo.PersistedTechnicians);
        Assert.Empty(keycloakRepo.Users);
    }

    [Fact]
    public void Create_WhenEmailAlreadyExistsInKeycloak_ReturnsConflictAndDoesNotPersist()
    {
        // Arrange
        var techRepo = new FakeTechnicianRepository();
        var keycloakRepo = new FakeKeycloakUserRepository();
        keycloakRepo.Users.Add(new KeycloakUser(
            Id: "kc-1",
            Username: "admin",
            FullName: "Admin SolarUY",
            Email: "admin@solaruy.local",
            Enabled: true,
            Role: Roles.Administrator));

        var service = new TechnicianService(techRepo, keycloakUserRepository: keycloakRepo, logger: NullLogger<TechnicianService>.Instance);
        var request = new CreateTechnicianRequest
        {
            FullName = "Nuevo Técnico",
            DocumentId = "12345678-A",
            Email = "ADMIN@solaruy.local",
            PhoneNumber = "+5491112345678",
            Password = "Password123"
        };

        // Act
        var result = service.Create(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal("El correo electrónico \"ADMIN@solaruy.local\" ya se encuentra registrado.", result.ErrorMessage);
        Assert.Empty(techRepo.PersistedTechnicians);
        Assert.Equal(0, keycloakRepo.CreateCalls);
    }

    [Fact]
    public void Create_WhenKeycloakIsUnavailable_ReturnsUnavailableAndDoesNotPersistTechnician()
    {
        // Arrange
        var techRepo = new FakeTechnicianRepository();
        var keycloakRepo = new FakeKeycloakUserRepository { IsUnavailable = true };
        var service = new TechnicianService(techRepo, keycloakUserRepository: keycloakRepo, logger: NullLogger<TechnicianService>.Instance);
        var request = new CreateTechnicianRequest
        {
            FullName = "Juan Pérez",
            DocumentId = "12345678-A",
            Email = "juan.perez@email.com",
            PhoneNumber = "+5491112345678",
            Password = "Password123"
        };

        // Act
        var result = service.Create(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Unavailable, result.ErrorType);
        Assert.Equal("No se pudo comunicar con el servicio de autenticación. Intentá nuevamente.", result.ErrorMessage);
        Assert.Empty(techRepo.PersistedTechnicians);
    }
}
