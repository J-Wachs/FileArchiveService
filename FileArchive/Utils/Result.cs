namespace FileArchive.Utils;

/// <summary>
/// Class that implements the Result-pattern to make it simple to 
/// return data and/or the result (errors).
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public IList<string> Messages { get; } = [];
    public T? Data { get; }

    private Result(bool isSuccess, IList<string> messages, T? data)
    {
        IsSuccess = isSuccess;
        Messages = messages;
        Data = data;

    }
    public static Result<T> Success() => new(true, [], default);
    public static Result<T> Success(T data) => new(true, [], data);
    public static Result<T> Success(T data, IList<string> messages) => new(true, messages, data);
    public static Result<T> Failure(string message) => new(false, [message], default);
    public static Result<T> Failure(IList<string> messages) => new(false, messages, default);
    public static Result<T> Failure(T? data, IList<string> messages) => new(false, messages, data);
}
