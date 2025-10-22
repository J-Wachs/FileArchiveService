using FileArchive.Interfaces;
using FileArchive.Utils;
using Microsoft.AspNetCore.Components.Forms;

namespace FileArchive.Services;

/// <summary>
/// Class for file handling in folder storage.
/// 
/// You must update the connection string in the appsetting.json file.
/// </summary>
public class FileArchiveStorageFolder : IFileArchiveStorage
{
    private readonly string? _targetPath;
    private readonly int _secondsBeforeReleaseFile;
    private readonly ILogger<FileArchiveStorageFolder> _logger;
    private readonly IFileArchiveFileInfoCRUD _fileInfoCRUD;
    private long _maxFileSize;

    public FileArchiveStorageFolder(ILogger<FileArchiveStorageFolder> logger, IConfiguration config, IFileArchiveFileInfoCRUD fileInfoCRUD)
    {
        _maxFileSize = ConfigHelper.GetMustExistConfigValue<long>(config, FileArchiveConstants.ConfigMaxFileSize);
        _targetPath = ConfigHelper.GetMustExistConfigValue<string>(config, FileArchiveConstants.ConfigPath);
        if (string.IsNullOrEmpty(_targetPath) || Path.Exists(_targetPath) is false)
        {
            throw new DirectoryNotFoundException(_targetPath);
        }
        _secondsBeforeReleaseFile = config.GetValue<int>(FileArchiveConstants.ConfigSecondsBeforeReleaseOfFile, 0);
        _logger = logger;
        _fileInfoCRUD = fileInfoCRUD;
    }

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
            var fileReleaseDateTime = fileInfo.Created.AddSeconds(_secondsBeforeReleaseFile);

            if (fileReleaseDateTime > DateTime.Now)
            {
                return Result<Stream?>.FailureForbidden($"The file with Id {id}, has not yet been released. It will be released at {fileReleaseDateTime}.");
            }

            var filePath = Path.Combine(_targetPath!, id.ToString());

            FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            await Task.CompletedTask;
            return Result<Stream?>.Success(fileStream);
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Error occurred in '{methodName}{paramList}'. The error is: 'Exception occurred '{ex}''.", methodName, paramList, ex);
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
            _logger.LogCritical("Error occurred in '{methodName}{paramList}'. The error is: 'Exception occurred '{ex}''.", methodName, paramList, ex);
            return Result.Fatal($"An error occurred");
        }
    }

    public async ValueTask<Result> StoreFile(long id, IBrowserFile file)
    {
        string methodName = nameof(StoreFile), paramList = $"({id}, file)";

        try
        {
            var filePath = Path.Combine(_targetPath!, id.ToString());

            await using FileStream output = new(filePath, FileMode.Create);
            await file.OpenReadStream(_maxFileSize).CopyToAsync(output);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Error occurred in '{methodName}{paramList}'. The error is: 'Exception occurred '{ex}''.", methodName, paramList, ex);
            return Result.Fatal("An error occurred");
        }
    }

    public async ValueTask<Result> DeleteStoredFile(long id)
    {
        string methodName = nameof(DeleteStoredFile), paramList = $"({id})";

        try
        {
            await Task.Run(() =>
            {
                var filePath = Path.Combine(_targetPath!, id.ToString());
                File.Delete(filePath);
            });

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Error occurred in '{methodName}{paramList}'. The error is: 'Exception occurred '{ex}''.", methodName, paramList, ex);
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
