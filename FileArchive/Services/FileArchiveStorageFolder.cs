using FileArchive.Interfaces;
using FileArchive.Utils;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;

namespace FileArchive.Services;

/// <summary>
/// Class for file handling in folder storage.
/// 
/// You must update the connection string in the appsetting.json file.
/// </summary>
public class FileArchiveStorageFolder : IFileArchiveStorage
{
    private readonly string? _targetPath;
    private long _maxFileSize;

    public FileArchiveStorageFolder(IConfiguration config)
    {
        _maxFileSize = ConfigHelper.GetMustExistConfigValue<long>(config, FileArchiveConstants.ConfigMaxFileSize);
        _targetPath = ConfigHelper.GetMustExistConfigValue<string>(config, FileArchiveConstants.ConfigPath);
        if (Path.Exists(_targetPath) is false)
        {
            throw new DirectoryNotFoundException(_targetPath);
        }
    }

    public async ValueTask<Result<Stream?>> OpenStoredFile(long id)
    {
        string methodName = $"{nameof(OpenStoredFile)}", paramList = $"({id})";

        try
        {
            if (_targetPath is null)
            {
                return Result<Stream?>.Failure($"Error occurred in '{methodName}', The error is: 'The TargetPath is not defined'.");
            }
            var filePath = Path.Combine(_targetPath, id.ToString());

            FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            await Task.CompletedTask;
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
        string methodName = $"{nameof(StoreFile)}", paramList = $"({id}, file)";

        try
        {
            if (_targetPath is null)
            {
                return Result.Failure($"Error occurred in '{methodName}', The error is: 'The TargetPath is not defined'.");
            }

            var filePath = Path.Combine(_targetPath, id.ToString());

            await using FileStream output = new(filePath, FileMode.Create);
            await file.OpenReadStream(_maxFileSize).CopyToAsync(output);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error occurred in '{methodName}{paramList}', The error is: '{ex}'.");
        }
    }

    public async ValueTask<Result> DeleteStoredFile(long id)
    {
        string methodName = $"{nameof(DeleteStoredFile)}", paramList = $"({id})";

        try
        {
            await Task.Run(() =>
            {
                if (_targetPath is not null)
                {
                    var filePath = Path.Combine(_targetPath, id.ToString());
                    File.Delete(filePath);
                }
            });

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error occurred in '{methodName}{paramList} ', The error is: '{ex}'.");
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
