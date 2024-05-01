using FileArchive.Models;

namespace FileArchive;

public interface IFileArchiveFileInfoCRUD
{
    void CreateFileInfo(FileArchiveInfo fileInfo, string userId);
    void UpdateFileInfo(FileArchiveInfo fileInfo, string userId);
    void DeleteFileInfo(long id);
    IList<FileArchiveInfo> GetListOfFileInfoByParentKey(string parentKey);
    FileArchiveInfo? GetFileInfoById(long id);
}
