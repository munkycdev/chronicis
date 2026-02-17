namespace Chronicis.Api.Models;

/// <summary>
/// Represents the status of a service operation.
/// </summary>
public enum ServiceStatus
{
    Success,
    NotFound,
    Forbidden,
    Conflict,
    ValidationError
}

/// <summary>
/// Wrapper for service operation results with explicit status.
/// Eliminates ambiguity between "not found" and "access denied" scenarios.
/// </summary>
public class ServiceResult<T>
{
    public ServiceStatus Status { get; init; }
    public T? Value { get; init; }
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Create a successful result with a value.
    /// </summary>
    public static ServiceResult<T> Success(T value) => new()
    {
        Status = ServiceStatus.Success,
        Value = value
    };

    /// <summary>
    /// Create a not found result.
    /// </summary>
    public static ServiceResult<T> NotFound(string? message = null) => new()
    {
        Status = ServiceStatus.NotFound,
        ErrorMessage = message ?? "Resource not found"
    };

    /// <summary>
    /// Create a forbidden result (insufficient permissions).
    /// </summary>
    public static ServiceResult<T> Forbidden(string? message = null) => new()
    {
        Status = ServiceStatus.Forbidden,
        ErrorMessage = message ?? "Access denied"
    };

    /// <summary>
    /// Create a conflict result (e.g., concurrency violation).
    /// </summary>
    public static ServiceResult<T> Conflict(string? message = null, T? currentValue = default) => new()
    {
        Status = ServiceStatus.Conflict,
        ErrorMessage = message ?? "Conflict detected",
        Value = currentValue
    };

    /// <summary>
    /// Create a validation error result.
    /// </summary>
    public static ServiceResult<T> ValidationError(string message) => new()
    {
        Status = ServiceStatus.ValidationError,
        ErrorMessage = message
    };
}
