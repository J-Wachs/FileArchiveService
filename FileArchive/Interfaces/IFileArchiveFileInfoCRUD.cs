using FileArchive.Models;
using FileArchive.Utils;

namespace FileArchive.Interfaces;

/// <summary>
/// Interface for the storage of information about files in the archive.
/// </summary>
public interface IFileArchiveFileInfoCRUD
{
    /// <summary>
    /// Adds a information about a new file to be added to the file archive.
    /// </summary>
    /// <param name="fileInfo">Information about the actual file</param>
    /// <param name="userId">Identification of the user that adds the file information</param>
    /// <returns></returns>
    ValueTask<Result> CreateFileInfo(FileArchiveInfo fileInfo, string userId);

    /// <summary>
    /// Updates information about a file in the file archive.
    /// </summary>
    /// <param name="fileInfo">Information about the actual file</param>
    /// <param name="userId">Identification of the user that updates the file information</param>
    /// <returns></returns>
    ValueTask<Result> UpdateFileInfo(FileArchiveInfo fileInfo, string userId);

    /// <summary>
    /// Deletes a file from the file archive.
    /// </summary>
    /// <param name="id">Id of the file information to be deleted.</param>
    /// <returns></returns>
    ValueTask<Result> DeleteFileInfo(long id);

    /// <summary>
    /// Retrieves a list of file information for files that have the supplied parent key.
    /// </summary>
    /// <param name="parentKey">Key to retrieve file information for</param>
    /// <returns></returns>
    ValueTask<Result<IList<FileArchiveInfo>?>> GetListOfFileInfoByParentKey(string parentKey);

    /// <summary>
    /// Retrieves information for a specific file.
    /// </summary>
    /// <param name="id">Id of the file to get file information for</param>
    /// <returns></returns>
    ValueTask<Result<FileArchiveInfo?>> GetFileInfoById(long id);
}
