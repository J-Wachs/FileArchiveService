using FileArchive.DataAccess.Interfaces;
using FileArchive.Interfaces;
using FileArchive.Models;
using FileArchive.Utils;
using Microsoft.EntityFrameworkCore;

namespace FileArchive.Services;

/// <summary>
/// Class for maintaining FileInfo data in a table using Entity Framework.
/// This class is for a production environment.
/// </summary>
public class FileArchiveFileInfoCRUDDB(
    ILogger<FileArchiveFileInfoCRUDDB> logger,
    IFileArchiveContext dbContext
    ) : IFileArchiveFileInfoCRUD
{
    public async ValueTask<Result> CreateFileInfo(FileArchiveInfo fileInfo, string userId)
    {
        string methodName = nameof(CreateFileInfo), paramList = "(fileInfo, userId)";

        try
        {
            fileInfo.Created = DateTime.Now;
            fileInfo.CreatedBy = userId;
            fileInfo.LastModified = DateTime.Now;
            fileInfo.LastModifiedBy = userId;

            dbContext.FileArchiveInfos.Add(fileInfo);
            await dbContext.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogCritical("Error in '{methodName}{paramList}'. The error is: 'Exception occurred '{ex}''.", methodName, paramList, ex);
            return Result.Fatal($"An error occurred.");
        }
    }

    public async ValueTask<Result> UpdateFileInfo(FileArchiveInfo fileInfo, string userId)
    {
        string methodName = $"{nameof(UpdateFileInfo)}", paramList = "(fileInfo, userId)";

        try
        {
            fileInfo.LastModified = DateTime.Now;
            fileInfo.LastModifiedBy = userId;

            var entity = dbContext.Entry(fileInfo);

            // This has been done, because I use MOQ to test.
            if (entity is not null)
            {
                entity.State = EntityState.Modified;
            }

            dbContext.FileArchiveInfos.Update(fileInfo);
            await dbContext.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogCritical("Error in '{methodName}{paramList}'. The error is: 'Exception occurred '{ex}''.", methodName, paramList, ex);
            return Result.Fatal($"An error occurred");
        }
    }

    public async ValueTask<Result> DeleteFileInfo(long id)
    {
        string methodName = nameof(DeleteFileInfo), paramList = $"({id})";

        try
        {
            var fileInfo = await dbContext.FileArchiveInfos.SingleOrDefaultAsync(x => x.Id == id);
            if (fileInfo is null)
            {
                return Result.FailureNotFound($"Error occurred in '{methodName}', The error is: 'FileArchiveInfos record with id '{id}' is not found'.");
            }

            var res = dbContext.FileArchiveInfos.Remove(fileInfo);
            await dbContext.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogCritical("Error in '{methodName}{paramList}'. The error is: 'Exception occurred '{ex}''.", methodName, paramList, ex);
            return Result.Fatal($"An error occurred");
        }
    }

    public async ValueTask<Result<IList<FileArchiveInfo>?>> GetListOfFileInfoByParentKey(string parentKey)
    {
        return Result<IList<FileArchiveInfo>?>.Success(await dbContext.FileArchiveInfos
            .Where(rec => rec.ParentKey == parentKey)
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync());
    }

    public async ValueTask<Result<FileArchiveInfo?>> GetFileInfoById(long id)
    {
        string methodName = $"{nameof(GetFileInfoById)}";

        var record = await dbContext.FileArchiveInfos.SingleOrDefaultAsync(x => x.Id == id);

        if (record is null)
        {
            return Result<FileArchiveInfo?>.FailureNotFound($"Error occurred in '{methodName}', The error is: 'Record with key '´{id}' does not exist'.");
        }

        return Result<FileArchiveInfo?>.Success(record);
    }
}
