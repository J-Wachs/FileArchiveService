using FileArchive.Utils;
using Microsoft.AspNetCore.Components.Forms;

namespace FileArchive.Interfaces;

/// <summary>
/// Interface for the storage of the actual file.
/// </summary>
public interface IFileArchiveStorage
{
    /// <summary>
    /// Store the actual file.
    /// </summary>
    /// <param name="id">Id of the file in the storage</param>
    /// <param name="file">Structure about the actual file uploaded</param>
    /// <returns></returns>
    ValueTask<Result> StoreFile(long id, IBrowserFile file);

    /// <summary>
    /// Opens a file in the storage.
    /// </summary>
    /// <param name="id">Id of the file to open</param>
    /// <returns>Stream to the file</returns>
    ValueTask<Result<Stream?>> OpenStoredFile(long id);

    /// <summary>
    /// Closes the stream to a file previously opened in the storage.
    /// </summary>
    /// <param name="stream">The file stream to close</param>
    /// <returns></returns>
    Result CloseStoredFile(Stream stream);

    /// <summary>
    /// Deletes a file from the storage.
    /// </summary>
    /// <param name="id">Id of the file to delete</param>
    /// <returns></returns>
    ValueTask<Result> DeleteStoredFile(long id);

    /// <summary>
    /// Sets the maximum size of a file that can be stored in the archive.
    /// </summary>
    /// <param name="maxFileSize"></param>
    void SetMaxFileSize(long maxFileSize);
}
