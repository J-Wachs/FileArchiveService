namespace FileArchive.Utils;

/// <summary>
/// Abstraction for Result and Result<> classes.
/// </summary>
public abstract class AbstractResult
{
    public bool IsSuccess { get; private set; }
    public IList<string> Messages { get; private set; } = [];
    protected AbstractResult(bool isSuccess, IList<string> messages)
    {
        IsSuccess = isSuccess;
        Messages = messages;
    }
}

/// <summary>
/// Class that implements the Result-pattern to make it simple to 
/// return the result and messages (errors).
/// </summary>
public class Result : AbstractResult
{
    private Result(bool isSuccess, IList<string> messages): base(isSuccess, messages)
    {
    }

    public Result And(Result result2) =>
        IsSuccess && result2.IsSuccess ? Success([.. Messages, .. result2.Messages]) : Failure([.. Messages, .. result2.Messages]);

    public static Result Success() => new(true, []);
    public static Result Success(string message) => new(true, [message]);
    public static Result Success(IList<string> messages) => new(true, messages);
    public static Result Failure(string message) => new(false, [message]);
    public static Result Failure(IList<string> messages) => new(false, messages);
}

/// <summary>
/// Class that implements the Result-pattern to make it simple to 
/// return data and/or the result (errors).
/// </summary>
public class Result<T> : AbstractResult
{
    public T? Data { get; }

    private Result(bool isSuccess, IList<string> messages, T? data) : base(isSuccess, messages)
    {
        Data = data;
    }
    public static Result<T> Success() => new(true, [], default);
    public static Result<T> Success(T data) => new(true, [], data);
    public static Result<T> Success(T data, IList<string> messages) => new(true, messages, data);
    public static Result<T> Failure(string message) => new(false, [message], default);
    public static Result<T> Failure(IList<string> messages) => new(false, messages, default);
    public static Result<T> Failure(T? data, IList<string> messages) => new(false, messages, data);
}
