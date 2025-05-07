using static FileArchive.Utils.FileArchiveJWTokenHelperRead;

namespace FileArchive.Utils.Interfaces;

/// <summary>
/// Interface for helpers to reading the Java Web Token.
/// </summary>
public interface IFileArchiveJWTokenHelperRead
{
    /// <summary>
    /// Retrieves the user id and file id from the Java Wewb Token.
    /// </summary>
    /// <param name="jwToken">Token containing the information</param>
    /// <returns></returns>
    Result<UserIdAndFileIdDTO?> GetUserIdAndFileIdFromJWToken(string jwToken);
}
