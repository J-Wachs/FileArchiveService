using FileArchive.DataAccess;
using FileArchive.Models;
using Microsoft.EntityFrameworkCore;

namespace FileArchive;

/// <summary>
/// Class for maintaining FileInfo data in a JSON file.
/// This class is for development only, as the method I have chosen
/// will not work in a multi-user production environment.
/// 
/// For production environments, use the class that stores the 
/// FileInfo data in a table. You can develop your own class for your
/// specific purpose.
/// </summary>
public class FileArchiveFileInfoCRUDDB(IFileArchiveContext dbContext) : IFileArchiveFileInfoCRUD
{
    public void CreateFileInfo(FileArchiveInfo fileInfo, string userId)
    {
        fileInfo.Created = DateTime.Now;
        fileInfo.CreatedBy = userId;
        fileInfo.LastModified = DateTime.Now;
        fileInfo.LastModifiedBy = userId;

        dbContext.FileArchiveInfos.Add(fileInfo);
        dbContext.SaveChanges();
    }

    public void UpdateFileInfo(FileArchiveInfo fileInfo, string userId)
    {
        fileInfo.LastModified = DateTime.Now;
        fileInfo.LastModifiedBy = userId;

        var entity = dbContext.Entry(fileInfo);
        
        if (entity is not null) // This has been done, because I use MOQ to test.
        {
            entity.State = EntityState.Modified;
        }

        dbContext.FileArchiveInfos.Update(fileInfo);
        dbContext.SaveChanges();
    }

    public void DeleteFileInfo(long id)
    {
        var fileInfo = dbContext.FileArchiveInfos.SingleOrDefault(x => x.Id == id);
        if (fileInfo is not null)
        {
            dbContext.FileArchiveInfos.Remove(fileInfo);
            dbContext.SaveChanges();
        }
        else
        {
            throw new KeyNotFoundException($"FileArchiveInfos record with id '{id}' is not found.");
        }
    }

    public IList<FileArchiveInfo> GetListOfFileInfoByParentKey(string parentKey)
    {
        return dbContext.FileArchiveInfos
            .Where(rec => rec.ParentKey == parentKey)
            .AsNoTrackingWithIdentityResolution()
            .ToList();
    }

    public FileArchiveInfo? GetFileInfoById(long id)
    {
        return dbContext.FileArchiveInfos.SingleOrDefault(x => x.Id == id);
    }

}
