using FileArchive.Interfaces;
using FileArchive.Models;
using FileArchive.Utils;

namespace FileArchive.Services;

public class FileArchiveCRUD(IFileArchiveFileInfoCRUD fileArchiveFileInfoCRUD, IFileArchiveStorage fileArchiveStorage) : IFileArchiveCRUD
{
    public async Task<Result> CreateUpdateDeleteArchiveFromUI(string parentKey, FileArchiveList fileArchiveList, string userId)
    {
        foreach (var oneFile in fileArchiveList.Files)
        {
            Result? result = default;
            if (oneFile.Insert is true)
            {
                result = await InsertFile(parentKey, userId, oneFile);
            }
            else if (oneFile.Delete is true)
            {
                result = await DeleteFile(oneFile);
            }
            else if (oneFile.Update is true)
                result = await UpdateFile(parentKey, userId, oneFile);

            // If all control flags are false, then we do nothing
            if (result is not null && result.IsSuccess is false)
            {
                return result;
            }
        }

        // Remove deleted entried:
        fileArchiveList.Files.RemoveAll(x => x.Delete == true);

        await fileArchiveList.RefreshData();

        return Result.Success();
    }


    public async Task<Result<List<FileArchiveFileInfoUI>>> GetListOfFileInfoUIForArchive(string parentKey)
    {
        Result<IList<FileArchiveInfo>?> getListResult = await fileArchiveFileInfoCRUD.GetListOfFileInfoByParentKey(parentKey);
        if (getListResult.IsSuccess is false)
        {
            return Result<List<FileArchiveFileInfoUI>>.Failure(getListResult.Messages);
        }

        List<FileArchiveFileInfoUI> listOfFilesForUI = [];
        foreach(var oneFileInfo in getListResult.Data!)
        {
            listOfFilesForUI.Add(new()
            {
                Id = oneFileInfo.Id,
                Filename = oneFileInfo.Filename,
                Description = oneFileInfo.Description,
                ParentKey = oneFileInfo.ParentKey,
                Created = oneFileInfo.Created,
                CreatedBy = oneFileInfo.CreatedBy,
                LastModified = oneFileInfo.LastModified,
                LastModifiedBy = oneFileInfo.LastModifiedBy,
                Delete = false,
                Insert = false,
                Update = false,
                File = null
            });
        }

        return Result<List<FileArchiveFileInfoUI>>.Success(listOfFilesForUI);
    }

	public async Task<Result> DeleteArchiveByParentKey(string parentKey)
	{
        Result<IList<FileArchiveInfo>?> getListResult = await fileArchiveFileInfoCRUD.GetListOfFileInfoByParentKey(parentKey);
        foreach (var fileInfo in getListResult.Data!)
        {
            Result result = (await fileArchiveStorage.DeleteStoredFile(fileInfo.Id))
                .And(await fileArchiveFileInfoCRUD.DeleteFileInfo(fileInfo.Id));

            if (result.IsSuccess is false)
            {
                return result;
            }
		}

        return Result.Success();
	}


    /// <summary>
    /// Adds one file to the file archive. Two step process: First add information then actual file.
    /// </summary>
    /// <param name="parentKey">Parent key to store file under</param>
    /// <param name="userId">Id of user adding the file</param>
    /// <param name="oneFile">Information about the file to add</param>
    /// <returns></returns>
    private async Task<Result> InsertFile(string parentKey, string userId, FileArchiveFileInfoUI oneFile)
    {
        // Add control record
        var fileInfo = new FileArchiveInfo
        {
            Filename = oneFile.Filename,
            FileMimeType = oneFile.File?.ContentType,
            Description = oneFile.Description,
            ParentKey = parentKey
        };

        Result result = await fileArchiveFileInfoCRUD.CreateFileInfo(fileInfo, userId);
        if (result.IsSuccess is false)
        {
            return result;
        }

        // Store file
        if (oneFile.File is not null)
        {
            oneFile.Id = fileInfo.Id;
            result = await fileArchiveStorage.StoreFile(fileInfo.Id, oneFile.File);
            oneFile.Insert = false;
            oneFile.Update = false;

            if (result.IsSuccess is false)
            {
                return result;
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Delete a file from the archive. Two step process: First remove actual file then remove information about the file.
    /// </summary>
    /// <param name="oneFile">The file to delete</param>
    /// <returns></returns>
    private async Task<Result> DeleteFile(FileArchiveFileInfoUI oneFile)
    {
        if (oneFile.Id is not null)
        {
            Result result = (await fileArchiveStorage.DeleteStoredFile(oneFile.Id.Value))
                .And(await fileArchiveFileInfoCRUD.DeleteFileInfo(oneFile.Id.Value));
            return result;
        }

        return Result.Success();
    }

    /// <summary>
    /// Update information about a file in the archive.
    /// </summary>
    /// <param name="parentKey">The parent key for the file</param>
    /// <param name="userId">The id of the user updating the file information</param>
    /// <param name="oneFile">Information about the file</param>
    /// <returns></returns>
    private async Task<Result> UpdateFile(string parentKey, string userId, FileArchiveFileInfoUI oneFile)
    {
        if (oneFile.Id is not null)
        {
            // Update control record 
            FileArchiveInfo theFile = new()
            {
                Id = oneFile.Id.Value,
                Filename = oneFile.Filename,
                Description = oneFile.Description,
                ParentKey = parentKey,
                Created = oneFile.Created is null ? DateTime.Now : oneFile.Created.Value,
                CreatedBy = oneFile.CreatedBy is null ? string.Empty : oneFile.CreatedBy
            };

            Result updateFileResult = await fileArchiveFileInfoCRUD.UpdateFileInfo(theFile, userId);
            if (updateFileResult.IsSuccess is false)
            {
                return updateFileResult;
            }

            oneFile.Update = false;
        }

        return Result.Success();
    }
}
