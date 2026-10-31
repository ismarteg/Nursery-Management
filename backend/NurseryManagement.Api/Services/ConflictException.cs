namespace NurseryManagement.Api.Services;

/// <summary>
/// Thrown by services when a unique/duplicate constraint is violated.
/// Controllers map this to HTTP 409 Conflict.
/// </summary>
public class ConflictException(string message) : Exception(message);
