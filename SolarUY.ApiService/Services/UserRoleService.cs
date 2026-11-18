using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Repositories;
using SolarUY.ApiService.Security;

namespace SolarUY.ApiService.Services;

public class UserRoleService : IUserRoleService
{
    private const string UnavailableMessage = "No se pudo comunicar con el servicio de autenticación. Intentá nuevamente.";

    private readonly IKeycloakUserRepository _repository;
    private readonly ITechnicianRepository? _technicianRepository;
    private readonly IMaintenanceRepository? _maintenanceRepository;
    private readonly ILogger<UserRoleService> _logger;

    public UserRoleService(
        IKeycloakUserRepository repository,
        ILogger<UserRoleService> logger,
        ITechnicianRepository? technicianRepository = null,
        IMaintenanceRepository? maintenanceRepository = null)
    {
        _repository = repository;
        _logger = logger;
        _technicianRepository = technicianRepository;
        _maintenanceRepository = maintenanceRepository;
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

            // Spec 039 RF-13: si pasa de Técnico a Administrador, verificar que no tenga mantenimientos pendientes.
            Technician? existingTech = null;
            if (_technicianRepository != null && !string.IsNullOrWhiteSpace(user.Email))
            {
                existingTech = _technicianRepository.GetByEmail(user.Email);
                if (user.Role == Roles.Technician && request.Role == Roles.Administrator && existingTech != null)
                {
                    var pendingCount = _maintenanceRepository?.CountPendingByTechnician(existingTech.Id) ?? 0;
                    if (pendingCount > 0)
                    {
                        return ServiceResult.Fail(
                            ServiceErrorType.Conflict,
                            $"No se puede cambiar el rol del técnico porque tiene {pendingCount} mantenimiento(s) pendiente(s) de resolución.");
                    }
                }
            }

            _repository.ReplaceSystemRole(userId, request.Role);

            if (_technicianRepository != null && !string.IsNullOrWhiteSpace(user.Email))
            {
                if (request.Role == Roles.Administrator && existingTech is { Status: TechnicianStatus.Active })
                {
                    existingTech.Status = TechnicianStatus.Inactive;
                    _technicianRepository.Update(existingTech);
                }
                else if (request.Role == Roles.Technician)
                {
                    if (existingTech != null)
                    {
                        if (existingTech.Status != TechnicianStatus.Active)
                        {
                            existingTech.Status = TechnicianStatus.Active;
                            _technicianRepository.Update(existingTech);
                        }
                    }
                    else
                    {
                        var trimmedEmail = user.Email.Trim();
                        var normalizedEmail = trimmedEmail.ToLowerInvariant();
                        var docSuffix = user.Id.Replace("-", "").ToLowerInvariant();
                        if (docSuffix.Length > 15)
                        {
                            docSuffix = docSuffix[..15];
                        }
                        var docId = $"kc{docSuffix}";

                        _technicianRepository.Add(new Technician
                        {
                            Id = Guid.NewGuid(),
                            FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName.Trim(),
                            DocumentId = docId,
                            NormalizedDocumentId = docId,
                            Email = trimmedEmail,
                            NormalizedEmail = normalizedEmail,
                            PhoneNumber = "000000",
                            Status = TechnicianStatus.Active,
                            CreatedAtUtc = DateTime.UtcNow
                        });
                    }
                }
            }

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
