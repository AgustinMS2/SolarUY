using Microsoft.Extensions.Logging;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Repositories;

namespace SolarUY.ApiService.Services;

public class TechnicianService : ITechnicianService
{
    private readonly ITacticianRepository _technicianRepository;
    private readonly ILogger<TechnicianService> _logger;

    public TechnicianService(ITacticianRepository technicianRepository, ILogger<TechnicianService> logger)
    {
        _technicianRepository = technicianRepository;
        _logger = logger;
    }

    public ServiceResult<Guid> Create(CreateTechnicianRequest request)
    {
        // Validar nombre completo
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "El nombre completo es obligatorio.");
        }

        var trimmedName = request.FullName.Trim();
        if (trimmedName.Length > 150)
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "El nombre completo no puede superar los 150 caracteres.");
        }

        // Validar documento de identidad
        if (string.IsNullOrWhiteSpace(request.DocumentId))
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "El documento de identidad es obligatorio.");
        }

        var trimmedDocument = request.DocumentId.Trim();
        if (trimmedDocument.Length < 1 || trimmedDocument.Length > 20)
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "El documento de identidad debe tener entre 1 y 20 caracteres.");
        }

        // Normalizar documento (quitar puntos y guiones para validar unicidad)
        var normalizedDocument = trimmedDocument.ToLowerInvariant().Replace(".", "").Replace("-", "");

        if (_technicianRepository.ExistsByDocument(normalizedDocument))
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Conflict, $"Ya existe un técnico registrado con el documento \"{trimmedDocument}\".");
        }

        // Validar correo electrónico
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "El correo electrónico es obligatorio.");
        }

        var trimmedEmail = request.Email.Trim();

        // Validar formato de email simple
        if (!trimmedEmail.Contains("@") || !trimmedEmail.Contains("."))
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "El formato del correo electrónico no es válido.");
        }

        var normalizedEmail = trimmedEmail.ToLowerInvariant();

        if (_technicianRepository.ExistsByEmail(normalizedEmail))
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Conflict, $"El correo electrónico \"{trimmedEmail}\" ya se encuentra registrado.");
        }

        // Validar teléfono
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "El teléfono de contacto es obligatorio.");
        }

        var trimmedPhone = request.PhoneNumber.Trim();

        // Telefonos deben tener 6 a 20 caracteres numéricos y prefijos (+, -)
        var phoneDigits = trimmedPhone.Replace(" ", "").Replace("-", "").Replace("+", "");
        if (phoneDigits.Length < 6 || phoneDigits.Length > 20 || !phoneDigits.All(char.IsDigit))
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "El teléfono de contacto es obligatorio y debe tener un formato válido.");
        }

        // Crear el técnico
        var technician = new Technician
        {
            Id = Guid.NewGuid(),
            FullName = trimmedName,
            DocumentId = normalizedDocument,
            NormalizedDocumentId = normalizedDocument,
            Email = trimmedEmail,
            NormalizedEmail = normalizedEmail,
            PhoneNumber = trimmedPhone,
            Specialty = request.Specialty,
            Status = TechnicianStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };

        _technicianRepository.Add(technician);
        _logger.LogInformation("Técnico creado exitosamente con ID {TechId}", technician.Id);

        return ServiceResult<Guid>.Ok(technician.Id);
    }
}