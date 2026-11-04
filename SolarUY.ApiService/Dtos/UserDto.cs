namespace SolarUY.ApiService.Dtos;

public record UserDto(
    string Id,
    string Username,
    string FullName,
    string Email,
    bool Enabled,
    string Role
);
