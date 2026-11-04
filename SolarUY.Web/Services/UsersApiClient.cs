using System.Net.Http.Json;

namespace SolarUY.Web.Services;

public record UserDto(
    string Id,
    string Username,
    string FullName,
    string Email,
    bool Enabled,
    string Role
);

public record ChangeUserRoleDto(string Role);

public class UsersApiResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public IReadOnlyList<UserDto> Users { get; set; } = [];
}

public class UsersApiClient(HttpClient httpClient)
{
    public async Task<UsersApiResult> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync("/api/users", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var users = await response.Content.ReadFromJsonAsync<List<UserDto>>(cancellationToken: cancellationToken);
                return new UsersApiResult { Success = true, Users = users ?? [] };
            }

            return new UsersApiResult { Success = false, ErrorMessage = await ReadErrorAsync(response, "Error al cargar los usuarios", cancellationToken) };
        }
        catch (Exception ex)
        {
            return new UsersApiResult { Success = false, ErrorMessage = $"No se pudo comunicar con el servicio: {ex.Message}" };
        }
    }

    public async Task<UsersApiResult> ChangeRoleAsync(string id, string role, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync(
                $"/api/users/{Uri.EscapeDataString(id)}/role", new ChangeUserRoleDto(role), cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new UsersApiResult { Success = true };
            }

            return new UsersApiResult { Success = false, ErrorMessage = await ReadErrorAsync(response, "Error al actualizar el rol", cancellationToken) };
        }
        catch (Exception ex)
        {
            return new UsersApiResult { Success = false, ErrorMessage = $"No se pudo comunicar con el servicio: {ex.Message}" };
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, string fallback, CancellationToken cancellationToken)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                return error.Message;
            }
        }
        catch (Exception)
        {
            // The body was empty or not JSON: fall back to the generic text below.
        }

        return $"{fallback} ({(int)response.StatusCode}).";
    }

    private sealed record ErrorResponse(string? Message);
}
