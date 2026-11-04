using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Repositories;
using SolarUY.ApiService.Security;

namespace SolarUY.ApiService.Services;

public class UserRoleService : IUserRoleService
{
    private const string UnavailableMessage = "No se pudo comunicar con el servicio de autenticación. Intentá nuevamente.";

    private readonly IKeycloakUserRepository _repository;
    private readonly ILogger<UserRoleService> _logger;

    public UserRoleService(IKeycloakUserRepository repository, ILogger<UserRoleService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public ServiceResult<IReadOnlyList<UserDto>> GetAll()
    {
        IReadOnlyList<KeycloakUser> users;
        try
        {
            users = _repository.GetAll();
        }
        catch (AuthenticationServiceUnavailableException ex)
        {
            _logger.LogWarning(ex, "Users could not be listed: authentication service unavailable");
            return ServiceResult<IReadOnlyList<UserDto>>.Fail(ServiceErrorType.Unavailable, UnavailableMessage);
        }

        IReadOnlyList<UserDto> dtos = users
            .OrderBy(u => u.Username, StringComparer.OrdinalIgnoreCase)
            .Select(u => new UserDto(u.Id, u.Username, u.FullName, u.Email, u.Enabled, u.Role ?? string.Empty))
            .ToList();

        return ServiceResult<IReadOnlyList<UserDto>>.Ok(dtos);
    }

    public ServiceResult ChangeRole(string userId, ChangeUserRoleRequest request)
    {
        if (request.Role != Roles.Administrator && request.Role != Roles.Technician)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "El rol seleccionado no es válido.");
        }

        try
        {
            var user = _repository.GetById(userId);
            if (user == null)
            {
                return ServiceResult.Fail(ServiceErrorType.NotFound, "El usuario solicitado no existe.");
            }

            // RF-5: the system must always keep at least one active administrator.
            var removesAdministratorRole = user.Role == Roles.Administrator && request.Role != Roles.Administrator;
            if (removesAdministratorRole && user.Enabled && !HasAnotherActiveAdministrator(userId))
            {
                return ServiceResult.Fail(
                    ServiceErrorType.Conflict,
                    "No se puede quitar el rol de Administrador al último administrador activo del sistema.");
            }

            _repository.ReplaceSystemRole(userId, request.Role);
            _logger.LogInformation("Role of user {UserId} changed to {Role}", userId, request.Role);
            return ServiceResult.Ok();
        }
        catch (AuthenticationServiceUnavailableException ex)
        {
            _logger.LogWarning(ex, "Role of user {UserId} could not be changed: authentication service unavailable", userId);
            return ServiceResult.Fail(ServiceErrorType.Unavailable, UnavailableMessage);
        }
    }

    private bool HasAnotherActiveAdministrator(string userId)
    {
        return _repository.GetAll().Any(u => u.Id != userId && u.Enabled && u.Role == Roles.Administrator);
    }
}
