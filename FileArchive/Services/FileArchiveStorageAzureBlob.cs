using Azure.Storage.Blobs;
using FileArchive.Interfaces;
using FileArchive.Utils;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;

namespace FileArchive.Services;

/// <summary>
/// Class for file handling in Azure Blob storage.
/// 
/// You must update the connection string in the appsetting.json file.
/// </summary>
public class FileArchiveFolderAzureBlob : IFileArchiveStorage
{
    private readonly string _blobConnectionString;
    private readonly string _blobStorageFolder;
    private long _maxFileSize;

    public FileArchiveFolderAzureBlob(IConfiguration config)
    {
        _maxFileSize = ConfigHelper.GetMustExistConfigValue<long>(config, FileArchiveConstants.ConfigMaxFileSize);
        _blobConnectionString = ConfigHelper.GetMustExistConfigValue<string>(config, FileArchiveConstants.AzureBlobConnectionString);
        // Azure Blob storage folders must have names in lower case.
        _blobStorageFolder = ConfigHelper.GetMustExistConfigValue<string>(config, FileArchiveConstants.BlobStorageFolder).ToLowerInvariant();
    }

    public async ValueTask<Result<Stream?>> OpenStoredFile(long id)
    {
        string methodName = $"{nameof(OpenStoredFile)}", paramList = $"({id})";

        try
        {
            var blobContainerClient = new BlobContainerClient(_blobConnectionString, _blobStorageFolder);

            var blob = blobContainerClient.GetBlobClient(id.ToString());

            Stream fileStream = await blob.OpenReadAsync();

            return Result<Stream?>.Success(fileStream);
        }
        catch (Exception ex)
        {
            return Result<Stream?>.Failure($"Error occurred in '{methodName}{paramList}', The error is: '{ex}'.");
        }
    }

    public Result CloseStoredFile(Stream stream)
    {
        string methodName = $"{nameof(CloseStoredFile)}", paramList = "(stream)";

        try
        {
            stream.Close();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error occurred in '{methodName}{paramList}', The error is: '{ex}'.");
        }
    }

    public async ValueTask<Result> StoreFile(long id, IBrowserFile file)
    {
        string methodName = $"", paramList = $"({id}, file)";

        try
        {
            var blobContainerClient = new BlobContainerClient(_blobConnectionString, _blobStorageFolder);

            Stream myBlob = file.OpenReadStream(_maxFileSize);

            await blobContainerClient.CreateIfNotExistsAsync();

            var blob = blobContainerClient.GetBlobClient(id.ToString());

            await blob.UploadAsync(myBlob);

            return Result.Success();
        }
        catch (Exception ex )
        {
            return Result.Failure($"Error occurred in '{methodName}{paramList}', The error is: '{ex}'.");
        }
    }

    public async ValueTask<Result> DeleteStoredFile(long id)
    {
        string methodName = $"{nameof(DeleteStoredFile)}", paramList = $"({id})";

        try
        {
            var blobContainerClient = new BlobContainerClient(_blobConnectionString, _blobStorageFolder);

            var blob = blobContainerClient.GetBlobClient(id.ToString());
            await blob.DeleteAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error occurred in '{methodName}{paramList}', The error is: '{ex}'.");
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
