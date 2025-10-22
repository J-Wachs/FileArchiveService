using System.Text.Json.Serialization;

namespace FileArchive.Utils;

public enum ResultCode
{
    Unknown = AbstractResult.Zero,
    Ok = StatusCodes.Status200OK,
    Created = StatusCodes.Status201Created,
    BadRequest = StatusCodes.Status400BadRequest,
    Unauthorized = StatusCodes.Status401Unauthorized,
    Forbidden = StatusCodes.Status403Forbidden,
    NotFound = StatusCodes.Status404NotFound,
    Conflict = StatusCodes.Status409Conflict,
    ServerError = StatusCodes.Status500InternalServerError
}

/// <summary>
/// Abstraction for Result and Result<> classes.
/// </summary>
public abstract class AbstractResult
{
    public const int Zero = 0;

    /// <summary>
    /// Result code of the result object.
    /// </summary>
    public ResultCode ResultCode { get; private set; }

    public bool IsSuccess { get; private set; }
    public IList<string> Messages { get; private set; } = [];

    /// <summary>
    /// Protected constructor for derived result classes.
    /// </summary>
    /// <param name="resultCode">The result code for the success/error state.</param>
    /// <param name="messages">An optional list of messages to associate with the result</param>
    protected AbstractResult(ResultCode resultCode, IList<string> messages)
    {
        IsSuccess = resultCode is ResultCode.Ok or ResultCode.Created;

        ResultCode = resultCode;
        Messages = messages;
    }
}

/// <summary>
/// Class that implements the Result-pattern to make it simple to 
/// return the result and messages (errors).
/// </summary>
public class Result : AbstractResult
{
    /// <summary>
    /// Private constructor to create a new generic result instance. Use static factory methods instead.
    /// </summary>
    /// <param name="resultCode">The result code for the success/error state.</param>
    /// <param name="messages">The list of messages associated with the result.</param>
    [JsonConstructor]
    private Result(ResultCode resultCode, IList<string> messages) : base(resultCode, messages)
    {
    }

    public Result And(Result result2) =>
        IsSuccess && result2.IsSuccess ? Success([.. Messages, .. result2.Messages]) : Failure([.. Messages, .. result2.Messages]);

    /// <summary>
    /// Creates a successful result with no messages.
    /// </summary>
    /// <returns>A new successful Result instance.</returns>
    public static Result Success() => new(ResultCode.Ok, []);
    public static Result Success(string message) => new(ResultCode.Ok, [message]);
    public static Result Success(IList<string> messages) => new(ResultCode.Ok, messages);
    public static Result Failure(string message) => new(ResultCode.BadRequest, [message]);
    public static Result Failure(IList<string> messages) => new(ResultCode.BadRequest, messages);

    /// <summary>
    /// Creates a failed bad request result with a single message associated with a specific field.
    /// </summary>
    /// <param name="message">The success message.</param>
    /// <returns>A new failed Result instance with a message.</returns>
    public static Result FailureBadRequest(string message) => new(ResultCode.BadRequest, [message]);

    /// <summary>
    /// Creates a failed not found result with a provided list of messages.
    /// </summary>
    /// <param name="message">The success message.</param>
    /// <returns>A new failed Result instance with messages.</returns>
    public static Result FailureNotFound(string message) => new(ResultCode.NotFound, [message]);

    /// <summary>
    /// Creates a fatal result with a single, general message. Data will be the default for type T.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A new failed Result instance.</returns>
    public static Result Fatal(string message) => new(ResultCode.ServerError, [message]);

    /// <summary>
    /// Creates a new Result instance by copying the state of another non-generic Result.
    /// </summary>
    /// <param name="result">The result to copy.</param>
    /// <returns>A new Result instance with the same properties.</returns>
    public static Result CopyResult(Result result) => new(result.ResultCode, result.Messages);

    /// <summary>
    /// Creates a new Result object with mwssages and result code from Result object in parameter.
    /// </summary>
    /// <typeparam name="TResult">The data type of the source result.</typeparam>
    /// <param name="result">The generic result to copy from.</param>
    /// <returns>A new non-generic Result instance.</returns>
    public static Result CopyResult<TResult>(Result<TResult> result) => new(result.ResultCode, result.Messages);
}

/// <summary>
/// Class that implements the Result-pattern to make it simple to 
/// return data and/or the result (errors).
/// </summary>
public class Result<T> : AbstractResult
{
    public T? Data { get; }

    /// <summary>
    /// Private constructor to create a new generic result instance. Use static factory methods instead.
    /// </summary>
    /// <param name="resultCode">The result code for the success/error state.</param>
    /// <param name="messages">The list of messages associated with the result.</param>
    /// <param name="data">The data payload of the result.</param>
    [JsonConstructor]
    private Result(ResultCode resultCode, IList<string> messages, T? data) : base(resultCode, messages)
    {
        Data = data;
    }

    public static Result<T> Success() => new(ResultCode.Ok, [], default);
    public static Result<T> Success(T data) => new(ResultCode.Ok, [], data);
    public static Result<T> Success(T data, IList<string> messages) => new(ResultCode.Ok, messages, data);
    public static Result<T> Failure(string message) => new(ResultCode.BadRequest, [message], default);
    public static Result<T> Failure(IList<string> messages) => new(ResultCode.BadRequest, messages, default);
    public static Result<T> Failure(T? data, IList<string> messages) => new(ResultCode.BadRequest, messages, data);

    /// <summary>
    /// Creates a unauthoried response result with a single, general message. Data will be the default for type T.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A new failed Result instance.</returns>
    public static Result<T> FailureUnauthorized(string message) => new(ResultCode.Unauthorized, [message], default);

    /// <summary>
    /// Creates a failed bad response result with a single, general message. Data will be the default for type T.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A new failed Result instance.</returns>
    public static Result<T> FailureBadRequest(string message) => new(ResultCode.BadRequest, [message], default);

    /// <summary>
    /// Creates a failed result with a single, general message. Data will be the default for type T.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A new failed Result instance.</returns>
    public static Result<T> FailureForbidden(string message) => new(ResultCode.Forbidden, [message], default);

    /// <summary>
    /// Creates a failed result with a single, general message. Data will be the default for type T.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A new failed Result instance.</returns>
    public static Result<T> FailureNotFound(string message) => new(ResultCode.NotFound, [message], default);

    /// <summary>
    /// Creates a fatal result with a single, general message. Data will be the default for type T.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A new failed Result instance.</returns>
    public static Result<T> Fatal(string message) => new(ResultCode.ServerError, [message], default);

    /// <summary>
    /// Creates a new generic Result by copying the state of another generic Result.
    /// </summary>
    /// <param name="result">The generic result to copy.</param>
    /// <returns>A new generic Result instance with the same properties.</returns>
    public static Result<T> CopyResult<T2>(Result<T2> result) => new(result.ResultCode, result.Messages, default);
}
