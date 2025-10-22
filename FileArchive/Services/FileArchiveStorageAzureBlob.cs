using Azure.Storage.Blobs;
using FileArchive.Interfaces;
using FileArchive.Utils;
using Microsoft.AspNetCore.Components.Forms;

namespace FileArchive.Services;

/// <summary>
/// Class for file handling in Azure Blob storage.
/// 
/// You must update the connection string in the appsetting.json file.
/// </summary>
public class FileArchiveFolderAzureBlob(
    ILogger<FileArchiveFolderAzureBlob> logger,
    IConfiguration config,
    IFileArchiveFileInfoCRUD fileInfoCRUD
    ) : IFileArchiveStorage
{
    private readonly string _blobConnectionString = ConfigHelper.GetMustExistConfigValue<string>(config, FileArchiveConstants.AzureBlobConnectionString);
    private readonly string _blobStorageFolder = ConfigHelper.GetMustExistConfigValue<string>(config, FileArchiveConstants.BlobStorageFolder).ToLowerInvariant();
    private readonly int _secondsBeforeReleaseFile = config.GetValue<int>(FileArchiveConstants.ConfigSecondsBeforeReleaseOfFile, 0);
    private readonly IFileArchiveFileInfoCRUD _fileInfoCRUD = fileInfoCRUD;
    private long _maxFileSize = ConfigHelper.GetMustExistConfigValue<long>(config, FileArchiveConstants.ConfigMaxFileSize);

    public async ValueTask<Result<Stream?>> OpenStoredFile(long id)
    {
        string methodName = nameof(OpenStoredFile), paramList = $"({id})";

        try
        {
            var getFileInfoByIdResult = await _fileInfoCRUD.GetFileInfoById(id);
            if (getFileInfoByIdResult.IsSuccess is false)
            {
                return Result<Stream?>.CopyResult(getFileInfoByIdResult);
            }

            var fileInfo = getFileInfoByIdResult.Data!;
            var fileReleaseDateTime = DateTime.Now.AddSeconds(-_secondsBeforeReleaseFile);

            if (fileInfo.Created > fileReleaseDateTime)
            {
                return Result<Stream?>.FailureForbidden($"The file with Id {id}, has not yet been released. It will be released at {fileReleaseDateTime}.");
            }

            var blobContainerClient = new BlobContainerClient(_blobConnectionString, _blobStorageFolder);

            var blob = blobContainerClient.GetBlobClient(id.ToString());

            Stream fileStream = await blob.OpenReadAsync();

            return Result<Stream?>.Success(fileStream);
        }
        catch (Exception ex)
        {
            logger.LogCritical("Error occurred in '{methodName}{paramList}'. The error is: 'Exception occurred '{ex}''.", methodName, paramList, ex);
            return Result<Stream?>.Fatal("An error occurred");
        }
    }

    public Result CloseStoredFile(Stream stream)
    {
        string methodName = nameof(CloseStoredFile), paramList = $"(stream)";

        try
        {
            stream.Close();
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogCritical("Error occurred in '{methodName}{paramList}'. The error is: 'Exception occurred '{ex}''.", methodName, paramList, ex);
            return Result.Fatal($"Error occurred in '{methodName}', The error is: '{ex}'.");
        }
    }

    public async ValueTask<Result> StoreFile(long id, IBrowserFile file)
    {
        string methodName = nameof(StoreFile), paramList = $"({id}, file)";

        try
        {
            var blobContainerClient = new BlobContainerClient(_blobConnectionString, _blobStorageFolder);

            Stream myBlob = file.OpenReadStream(_maxFileSize);

            await blobContainerClient.CreateIfNotExistsAsync();

            var blob = blobContainerClient.GetBlobClient(id.ToString());

            await blob.UploadAsync(myBlob);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogCritical("Error occurred in '{methodName}{paramList}'. The error is: 'Exception occurred '{ex}''.", methodName, paramList, ex);
            return Result.Fatal("An error occurred");
        }
    }

    public async ValueTask<Result> DeleteStoredFile(long id)
    {
        string methodName = nameof(DeleteStoredFile), paramList = $"({id})";

        try
        {
            var blobContainerClient = new BlobContainerClient(_blobConnectionString, _blobStorageFolder);

            var blob = blobContainerClient.GetBlobClient(id.ToString());
            await blob.DeleteAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogCritical("Error occurred in '{methodName}{paramList}'. The error is: 'Exception occurred '{ex}''.", methodName, paramList, ex);
            return Result.Fatal("An error occurred");
        }
    }

    public void SetMaxFileSize(long maxFileSize)
    {
        if (maxFileSize <= 0)
        {
            throw new ArgumentException("maxFileSize is 0 or less");
        }
        _maxFileSize = maxFileSize;
    }
}
