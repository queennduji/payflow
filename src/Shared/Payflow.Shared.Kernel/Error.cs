namespace Payflow.Shared.Kernel;

public enum ErrorType
{
    Failure,
    Validation,
    NotFound,
    Conflict
}

/// <summary>
/// A machine-readable, transport-agnostic description of why an operation failed. Deliberately not
/// an exception: expected business failures (declined authorization, invalid amount, duplicate
/// idempotency key) are part of the domain's vocabulary, not exceptional control flow. Carrying
/// <see cref="Type"/> lets each host (HTTP API, message consumer, ...) map to its own failure
/// representation without string-sniffing <see cref="Code"/>.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
}
