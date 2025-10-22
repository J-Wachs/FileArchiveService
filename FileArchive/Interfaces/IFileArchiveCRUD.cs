using FileArchive.Models;
using FileArchive.Utils;

namespace FileArchive.Interfaces;

public interface IFileArchiveCRUD
{
    /// <summary>
    /// Handles the actions user selects in the UI component.
    /// </summary>
    Task<Result> CreateUpdateDeleteArchiveFromUI(string parentKey, FileArchiveList fileArchiveList, string userId);

    /// <summary>
    /// Handles the actions user selects in the UI component.
    /// </summary>
    Task<Result> CreateUpdateDeleteArchiveFromUI(string parentKey, FileArchiveCards fileArchiveCards, string userId);


    /// <summary>
    /// Retrieves a list of info about all the files having the supplied parent key.
    /// </summary>
    /// <param name="parentKey">Key to retrieve file information for</param>
    /// <returns>List of file info for UI</returns>
    Task<Result<List<FileArchiveFileInfoUI>>> GetListOfFileInfoUIForArchive(string parentKey);

    /// <summary>
    /// Deletes file info and stored file for all files having the supplied parent key.
    /// </summary>
    /// <param name="parentKey">Key to delete file info and files for</param>
    /// <returns></returns>
    Task<Result> DeleteArchiveByParentKey(string parentKey);
}
