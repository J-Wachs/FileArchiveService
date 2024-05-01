using FileArchive.Models;

namespace FileArchive;

public class FileArchiveCRUD(IFileArchiveFileInfoCRUD jWFileArchiveFileInfoCRUD, IFileArchiveStorage jwFileArchiveStorage) : IFileArchiveCRUD
{
    public async Task<bool> CreateUpdateDeleteArchiveFromUI(string parentKey, List<FileArchiveFileInfoUI> fileArchiveListUI, string userId)
    {
        foreach(var oneFile in fileArchiveListUI)
        {
            if (oneFile.Insert)
            {
                // Add control record
                var fileInfo = new FileArchiveInfo
                {
                    Filename = oneFile.Filename,
                    FileMimeType = oneFile.File?.ContentType,
                    Description = oneFile.Description, 
                    ParentKey = parentKey
                };

                jWFileArchiveFileInfoCRUD.CreateFileInfo(fileInfo, userId);
                // Store file
                if (oneFile.File is not null)
                {
                    oneFile.Id = fileInfo.Id;
                    _ = await jwFileArchiveStorage.StoreFile(fileInfo.Id, oneFile.File);
                    oneFile.Insert = false;
                    oneFile.Update = false;
                }
            }
            else if (oneFile.Delete)
            {
                if (oneFile.Id is not null)
                {
                    // Remove file
                    await jwFileArchiveStorage.DeleteStoredFile(oneFile.Id.Value);

                    // Remove control record
                    jWFileArchiveFileInfoCRUD.DeleteFileInfo(oneFile.Id.Value);
                }
            }
            else if (oneFile.Update && oneFile.Id is not null)
            {
                // Update control record 
                FileArchiveInfo theFile = new()
                {
                    Id = (long)oneFile.Id,
                    Filename = oneFile.Filename,
                    Description = oneFile.Description,
                    ParentKey = parentKey,
                    Created = oneFile.Created is null ? DateTime.Now : (DateTime)oneFile.Created,
                    CreatedBy = oneFile.CreatedBy is null ? String.Empty : oneFile.CreatedBy
                };
                jWFileArchiveFileInfoCRUD.UpdateFileInfo(theFile, userId);
                oneFile.Update = false;
            }
            // If all control flags are false, then we do nothing
        }

        // Remove deleted entried:
        fileArchiveListUI.RemoveAll(x => x.Delete);

        return true;
    }

    public List<FileArchiveFileInfoUI> GetListOfFileInfoForArchive(string parentKey)
    {
        List<FileArchiveFileInfoUI> listOfFilesForUI = [];
        foreach(var oneFileInfo in jWFileArchiveFileInfoCRUD.GetListOfFileInfoByParentKey(parentKey))
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
                Insert = false,
                Update = false,
                Delete = false,
                File = null
            });
        }

        return listOfFilesForUI;
    }

	public async Task DeleteArchiveByParentKey(string parentKey)
	{
        foreach(var fileInfo in jWFileArchiveFileInfoCRUD.GetListOfFileInfoByParentKey(parentKey))
        {
            await jwFileArchiveStorage.DeleteStoredFile(fileInfo.Id);

            jWFileArchiveFileInfoCRUD.DeleteFileInfo(fileInfo.Id);
		}

        return;
	}
}
