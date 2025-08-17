namespace VDS.Contracts.CQRS;

/// <summary>
/// Base interface for all CQRS requests
/// </summary>
public interface IRequest
{
}

/// <summary>
/// Base interface for all CQRS requests with a response
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public interface IRequest<out TResponse> : IRequest
{
}

/// <summary>
/// Base interface for all command handlers
/// </summary>
/// <typeparam name="TCommand">The command type</typeparam>
public interface ICommandHandler<in TCommand> where TCommand : IRequest
{
    Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base interface for all command handlers with a response
/// </summary>
/// <typeparam name="TCommand">The command type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public interface ICommandHandler<in TCommand, TResponse> where TCommand : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base interface for all query handlers
/// </summary>
/// <typeparam name="TQuery">The query type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public interface IQueryHandler<in TQuery, TResponse> where TQuery : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base class for all commands
/// </summary>
public abstract record Command : IRequest;

/// <summary>
/// Base class for all commands with a response
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public abstract record Command<TResponse> : IRequest<TResponse>;

/// <summary>
/// Base class for all queries
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public abstract record Query<TResponse> : IRequest<TResponse>;

/// <summary>
/// Common result wrapper for operations that can fail
/// </summary>
/// <typeparam name="T">The result type</typeparam>
public record Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? ErrorMessage { get; init; }
    public Exception? Exception { get; init; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string errorMessage) => new() { IsSuccess = false, ErrorMessage = errorMessage };
    public static Result<T> Failure(Exception exception) => new() { IsSuccess = false, Exception = exception, ErrorMessage = exception.Message };
}

/// <summary>
/// Result wrapper for operations without a return value
/// </summary>
public record Result
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public Exception? Exception { get; init; }

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string errorMessage) => new() { IsSuccess = false, ErrorMessage = errorMessage };
    public static Result Failure(Exception exception) => new() { IsSuccess = false, Exception = exception, ErrorMessage = exception.Message };
}