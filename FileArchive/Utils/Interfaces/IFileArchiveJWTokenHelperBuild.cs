namespace FileArchive.Utils.Interfaces;

/// <summary>
/// Interface for helpers for Java Web Token.
/// </summary>
public interface IFileArchiveJWTokenHelperBuild
{
    /// <summary>
    /// Creates a Json Web Token to use for the Download API.
    /// </summary>
    /// <param name="userId">Id of the user that wants to downlaod</param>
    /// <param name="fileId">Id of the file to download</param>
    /// <returns></returns>
    Result<string> BuildTokenForFileDownload(string userId, long fileId);
}
