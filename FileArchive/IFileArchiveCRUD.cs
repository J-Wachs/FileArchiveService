namespace FileArchive;

public interface IFileArchiveCRUD
{
    Task<bool> CreateUpdateDeleteArchiveFromUI(string parentKey, List<FileArchiveFileInfoUI> fileArchiveListUI, string userId);
    List<FileArchiveFileInfoUI> GetListOfFileInfoForArchive(string parentKey);
    Task DeleteArchiveByParentKey(string parentKey);
}
