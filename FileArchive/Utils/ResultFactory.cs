namespace FileArchive.Utils;

/// <summary>
/// Class that implements the Result-pattern to make it simple to 
/// return data and/or the result (errors).
/// </summary>
internal class ResultFactory
{
    internal static ResultObject<T> GoodResult<T>(T result)
    {
        return GoodResult(result, []);
    }

    internal static ResultObject<T> GoodResult<T>(T result, List<string> messages)
    {
        return new ResultObject<T> { Success = true, Messages = messages, Result = result };
    }

    internal static ResultObject<T> BadResult<T>(string error)
    {
        List<string> messages = [error];
        return BadResult<T>(messages);
    }

    internal static ResultObject<T> BadResult<T>(List<string> errors)
    {
        return new ResultObject<T> { Success = false, Messages = errors };
    }

    internal static ResultObject<T> BadResult<T>(T? result, List<string> errors)
    {
        return new ResultObject<T> { Success = false, Result = result, Messages = errors };
    }
}
