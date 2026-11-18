using System.ComponentModel.DataAnnotations;

namespace SolarUY.ApiService.Dtos;

public class CreateTechnicianRequest
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre completo no puede superar los 150 caracteres.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El documento de identidad es obligatorio.")]
    [StringLength(20, ErrorMessage = "El documento de identidad no puede superar los 20 caracteres.")]
    public string DocumentId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono de contacto es obligatorio.")]
    [StringLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "La especialidad no puede superar los 100 caracteres.")]
    public string? Specialty { get; set; }

    [Required(ErrorMessage = "La contraseña es obligatoria y debe tener al menos 6 caracteres.")]
    [MinLength(6, ErrorMessage = "La contraseña es obligatoria y debe tener al menos 6 caracteres.")]
    [StringLength(100, ErrorMessage = "La contraseña no puede superar los 100 caracteres.")]
    public string Password { get; set; } = string.Empty;
}