using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Security;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class UserRoleServiceTests
{
    private const string UnavailableMessage = "No se pudo comunicar con el servicio de autenticación. Intentá nuevamente.";

    private readonly FakeKeycloakUserRepository _repository = new();
    private readonly UserRoleService _service;

    public UserRoleServiceTests()
    {
        _service = new UserRoleService(_repository, NullLogger<UserRoleService>.Instance);
    }

    private static KeycloakUser User(string id, string username, string? role, bool enabled = true)
    {
        return new KeycloakUser(id, username, $"Nombre {username}", $"{username}@solaruy.local", enabled, role);
    }

    // ---- GetAll (RF-6, RF-7)

    [Fact]
    public void GetAll_ReturnsAllUsersOrderedByUsernameIgnoringCase()
    {
        _repository.Users.Add(User("1", "tecnico", Roles.Technician));
        _repository.Users.Add(User("2", "Zeta", null));
        _repository.Users.Add(User("3", "admin", Roles.Administrator, enabled: false));

        var result = _service.GetAll();

        Assert.True(result.Success);
        Assert.Equal(["admin", "tecnico", "Zeta"], result.Value!.Select(u => u.Username));
    }

    [Fact]
    public void GetAll_MapsRoleAndEnabledFlag_AndShowsEmptyRoleWhenUserHasNone()
    {
        _repository.Users.Add(User("1", "admin", Roles.Administrator));
        _repository.Users.Add(User("2", "sinrol", null, enabled: false));

        var result = _service.GetAll();

        var admin = result.Value!.Single(u => u.Username == "admin");
        var withoutRole = result.Value!.Single(u => u.Username == "sinrol");
        Assert.Equal(Roles.Administrator, admin.Role);
        Assert.True(admin.Enabled);
        Assert.Equal(string.Empty, withoutRole.Role);
        Assert.False(withoutRole.Enabled);
    }

    [Fact]
    public void GetAll_WhenKeycloakIsUnavailable_ReturnsUnavailable()
    {
        _repository.IsUnavailable = true;

        var result = _service.GetAll();

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Unavailable, result.ErrorType);
        Assert.Equal(UnavailableMessage, result.ErrorMessage);
    }

    // ---- ChangeRole (RF-3, RF-7)

    [Fact]
    public void ChangeRole_WithValidRole_ReplacesTheUserRole()
    {
        _repository.Users.Add(User("1", "tecnico", Roles.Technician));

        var result = _service.ChangeRole("1", new ChangeUserRoleRequest(Roles.Administrator));

        Assert.True(result.Success);
        Assert.Equal(Roles.Administrator, _repository.GetById("1")!.Role);
        Assert.Equal(1, _repository.ReplaceCalls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Supervisor")]
    [InlineData("administrador")]
    public void ChangeRole_WithInvalidRole_ReturnsValidationAndChangesNothing(string? role)
    {
        _repository.Users.Add(User("1", "tecnico", Roles.Technician));

        var result = _service.ChangeRole("1", new ChangeUserRoleRequest(role));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El rol seleccionado no es válido.", result.ErrorMessage);
        Assert.Equal(Roles.Technician, _repository.GetById("1")!.Role);
        Assert.Equal(0, _repository.ReplaceCalls);
    }

    [Fact]
    public void ChangeRole_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var result = _service.ChangeRole("no-existe", new ChangeUserRoleRequest(Roles.Technician));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("El usuario solicitado no existe.", result.ErrorMessage);
        Assert.Equal(0, _repository.ReplaceCalls);
    }

    [Fact]
    public void ChangeRole_WhenKeycloakIsUnavailable_ReturnsUnavailable()
    {
        _repository.Users.Add(User("1", "tecnico", Roles.Technician));
        _repository.IsUnavailable = true;

        var result = _service.ChangeRole("1", new ChangeUserRoleRequest(Roles.Administrator));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Unavailable, result.ErrorType);
        Assert.Equal(UnavailableMessage, result.ErrorMessage);
    }

    // ---- Last active administrator (RF-5)

    private const string LastAdministratorMessage = "No se puede quitar el rol de Administrador al último administrador activo del sistema.";

    [Fact]
    public void ChangeRole_RemovingAdministratorFromTheOnlyActiveAdministrator_ReturnsConflictAndChangesNothing()
    {
        _repository.Users.Add(User("1", "admin", Roles.Administrator));
        _repository.Users.Add(User("2", "tecnico", Roles.Technician));

        var result = _service.ChangeRole("1", new ChangeUserRoleRequest(Roles.Technician));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal(LastAdministratorMessage, result.ErrorMessage);
        Assert.Equal(Roles.Administrator, _repository.GetById("1")!.Role);
        Assert.Equal(0, _repository.ReplaceCalls);
    }

    [Fact]
    public void ChangeRole_RemovingAdministratorWhenAnotherActiveAdministratorExists_Succeeds()
    {
        _repository.Users.Add(User("1", "admin", Roles.Administrator));
        _repository.Users.Add(User("2", "admin2", Roles.Administrator));

        var result = _service.ChangeRole("1", new ChangeUserRoleRequest(Roles.Technician));

        Assert.True(result.Success);
        Assert.Equal(Roles.Technician, _repository.GetById("1")!.Role);
    }

    [Fact]
    public void ChangeRole_DisabledAdministratorDoesNotCountAsActive()
    {
        _repository.Users.Add(User("1", "admin", Roles.Administrator));
        _repository.Users.Add(User("2", "admin2", Roles.Administrator, enabled: false));

        var result = _service.ChangeRole("1", new ChangeUserRoleRequest(Roles.Technician));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal(LastAdministratorMessage, result.ErrorMessage);
        Assert.Equal(Roles.Administrator, _repository.GetById("1")!.Role);
    }

    [Fact]
    public void ChangeRole_DemotingADisabledAdministrator_IsAllowedBecauseItIsNotAnActiveAdministrator()
    {
        _repository.Users.Add(User("1", "admin", Roles.Administrator));
        _repository.Users.Add(User("2", "viejo", Roles.Administrator, enabled: false));

        var result = _service.ChangeRole("2", new ChangeUserRoleRequest(Roles.Technician));

        Assert.True(result.Success);
        Assert.Equal(Roles.Technician, _repository.GetById("2")!.Role);
    }

    [Fact]
    public void ChangeRole_KeepingTheAdministratorRole_IsAllowedEvenForTheLastAdministrator()
    {
        _repository.Users.Add(User("1", "admin", Roles.Administrator));

        var result = _service.ChangeRole("1", new ChangeUserRoleRequest(Roles.Administrator));

        Assert.True(result.Success);
        Assert.Equal(Roles.Administrator, _repository.GetById("1")!.Role);
    }

    [Fact]
    public void ChangeRole_PromotingATechnician_IsNotAffectedByTheLastAdministratorRule()
    {
        _repository.Users.Add(User("1", "tecnico", Roles.Technician));

        var result = _service.ChangeRole("1", new ChangeUserRoleRequest(Roles.Administrator));

        Assert.True(result.Success);
    }
}
