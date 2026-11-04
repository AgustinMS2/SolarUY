namespace SolarUY.ApiService.Repositories;

/// <summary>
/// The authentication service (Keycloak) could not be reached or rejected the request.
/// </summary>
public class AuthenticationServiceUnavailableException : Exception
{
    public AuthenticationServiceUnavailableException(string message) : base(message)
    {
    }

    public AuthenticationServiceUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
