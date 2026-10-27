namespace SolarUY.ApiService.Services;

public enum ServiceErrorType
{
    None,
    Validation,
    NotFound,
    Conflict
}

public class ServiceResult
{
    public bool Success { get; init; }
    public ServiceErrorType ErrorType { get; init; }
    public string? ErrorMessage { get; init; }

    public static ServiceResult Ok() => new() { Success = true };
    public static ServiceResult Fail(ServiceErrorType type, string message)
        => new() { Success = false, ErrorType = type, ErrorMessage = message };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Value { get; init; }
    public static ServiceResult<T> Ok(T value) => new() { Success = true, Value = value };
    public static new ServiceResult<T> Fail(ServiceErrorType type, string message)
        => new() { Success = false, ErrorType = type, ErrorMessage = message };
}
