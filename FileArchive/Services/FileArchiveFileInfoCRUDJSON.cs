using FileArchive.Interfaces;
using FileArchive.Models;
using FileArchive.Utils;
using System.Text.Json;

namespace FileArchive.Services;

/// <summary>
/// Class for maintaining FileInfo data in a JSON file.
/// This class is for development only, as the method I have chosen
/// will not work in a multi-user production environment.
/// 
/// For production environments, use the class that stores the 
/// FileInfo data in a table. You can develop your own class for your
/// specific purpose.
/// </summary>
public class FileArchiveFileInfoCRUDJSON : IFileArchiveFileInfoCRUD
{
    private readonly string? _targetPath;
    private readonly string _targetJSONFile;
    private readonly ILogger<FileArchiveFileInfoCRUDJSON> _logger;
    private const int NotFound = -1;

    public FileArchiveFileInfoCRUDJSON(ILogger<FileArchiveFileInfoCRUDJSON> logger, IConfiguration config)
	{
        _targetPath = config.GetValue<string>(FileArchiveConstants.ConfigPath);
        if (_targetPath is null)
        {
            throw new DirectoryNotFoundException(FileArchiveConstants.ConfigPath);
        }

        if (!Path.Exists(_targetPath))
        {
            throw new DirectoryNotFoundException(_targetPath);
        }

		_targetJSONFile = Path.Combine(_targetPath, "FileInfo.json");
        _logger = logger;
    }

    public async ValueTask<Result> CreateFileInfo(FileArchiveInfo fileInfo, string userId)
    {
        fileInfo.Created = DateTime.Now;
        fileInfo.CreatedBy = userId;
        fileInfo.LastModified = DateTime.Now;
        fileInfo.LastModifiedBy = userId;

        Result<List<FileArchiveInfo>?> readListResult = await ReadListOfFiles();
        if (readListResult.IsSuccess is false)
        {
            return Result.CopyResult(readListResult);
        }
        var listOfFiles = readListResult.Data!;

        // Not that clever way to get next free fileId:
        long fileId = 0;
        var newFileId = listOfFiles.MaxBy(x => x.Id)?.Id;
        newFileId ??= 0;
        fileId = (long)newFileId + 1;

        fileInfo.Id = fileId;
        listOfFiles.Add(fileInfo);

        Result writeListResult = await WriteListOfFiles(listOfFiles);

        return writeListResult;
    }

    public async ValueTask<Result> UpdateFileInfo(FileArchiveInfo fileInfo, string userId)
    {
        string methodName = nameof(UpdateFileInfo), paramList = $"(fileInfo[Id={fileInfo.Id}], userId)";
        
        Result<List<FileArchiveInfo>?> readListOfFilesResult = await ReadListOfFiles();
        if (readListOfFilesResult.IsSuccess is false)
        {
            return Result.CopyResult(readListOfFilesResult);
        }
        var listOfFiles = readListOfFilesResult.Data!;

        var index = listOfFiles.FindIndex(x => x.Id == fileInfo.Id);
        if (index is NotFound)
        {
            _logger.LogError("Error occurred in '{methodName}{paramList}', The error is: 'The File Id {fileInfo.Id} is not found in JSON file'.", methodName, paramList, fileInfo.Id);
            return Result.FailureNotFound($"Error occurred in '{methodName}', The error is: 'The File Id {fileInfo.Id} is not found in JSON file'.");
        }

        fileInfo.LastModified = DateTime.Now;
        fileInfo.LastModifiedBy = userId;
        listOfFiles[index] = fileInfo;

        Result writeListResult = await WriteListOfFiles(listOfFiles);

        return writeListResult;
    }

    public async ValueTask<Result> DeleteFileInfo(long id)
    {
        string methodName = nameof(DeleteFileInfo), paramList = $"({id})";

        Result<List<FileArchiveInfo>?> readListResult = await ReadListOfFiles();
        if (readListResult.IsSuccess is false)
        {
            return Result.CopyResult(readListResult);
        }

        var fileInfo = readListResult.Data!.FirstOrDefault(x => x.Id == id);
        if (fileInfo is null)
        {
            _logger.LogError("Error occurred in '{methodName}{paramList}', The error is: 'The File Id {id} is not found in JSON file'.", methodName, paramList, id);
            return Result.FailureNotFound($"Error occurred in '{methodName}', The error is: 'The File Id {id} is not found in JSON file'.");
        }

        var listOfFiles = readListResult.Data!.Where(x => x.Id != id).ToList();
        Result writeListOfFilesResult = await WriteListOfFiles(listOfFiles);
        return writeListOfFilesResult;
    }

    public async ValueTask<Result<IList<FileArchiveInfo>?>> GetListOfFileInfoByParentKey(string parentKey)
    {
        Result<List<FileArchiveInfo>?> readListResult = await ReadListOfFiles();
        if (readListResult.IsSuccess is false)
        {
            return Result<IList<FileArchiveInfo>?>.CopyResult(readListResult);
        }
        var listOfFiles = readListResult.Data!;

        return Result<IList<FileArchiveInfo>?>.Success([.. listOfFiles.Where(x => x.ParentKey == parentKey)]);
    }

    public async ValueTask<Result<FileArchiveInfo?>> GetFileInfoById(long id)
    {
        Result<List<FileArchiveInfo>?> readListResult = await ReadListOfFiles();
        if (readListResult.IsSuccess is false)
        {
            return Result<FileArchiveInfo?>.CopyResult(readListResult);
        }

        return Result<FileArchiveInfo?>.Success(readListResult.Data!.FirstOrDefault(x => x.Id == id));
    }

    /// <summary>
    /// Reads a list of files from a JSON file and returns the deserialized result.
    /// </summary>
    /// <remarks>This method attempts to read and deserialize a JSON file specified by the internal target
    /// file path. If the file does not exist, an empty list is returned. If an error occurs during the operation, a
    /// fatal result containing the error details is returned.</remarks>
    /// <returns>A <see cref="Result{T}"/> containing a list of <see cref="FileArchiveInfo"/> objects if the operation succeeds,
    /// or a fatal result with error details if an exception occurs. The list will be empty if the file does not exist.</returns>
    private async ValueTask<Result<List<FileArchiveInfo>?>> ReadListOfFiles()
    {
        string methodName = nameof(ReadListOfFiles), paramList = "()";

        try
        {
            List<FileArchiveInfo>? listOfFiles = null;

            if (File.Exists(_targetJSONFile))
            {
                using FileStream createStream = File.OpenRead(_targetJSONFile);
                listOfFiles = await JsonSerializer.DeserializeAsync<List<FileArchiveInfo>>(createStream);
                createStream.Close();
            }

            listOfFiles ??= [];

            return Result<List<FileArchiveInfo>?>.Success(listOfFiles);
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Error in '{methodName}{paramList}'. The error is: 'Exception occurred '{ex}''.", methodName, paramList, ex);
            return Result<List<FileArchiveInfo>?>.Fatal("An error occurred");
        }
    }

    /// <summary>
    /// Writes a list of file metadata to a JSON file asynchronously.
    /// </summary>
    /// <remarks>This method creates or overwrites the target JSON file specified by the internal field
    /// <c>_targetJSONFile</c>. If an exception occurs during the operation, the method returns a fatal <see
    /// cref="Result"/> containing the error details.</remarks>
    /// <param name="listOfFiles">The list of <see cref="FileArchiveInfo"/> objects to be serialized and written to the target JSON file.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation. The result is a <see cref="Result"/>
    /// object indicating the success or failure of the operation.</returns>
    private async ValueTask<Result> WriteListOfFiles(IList<FileArchiveInfo> listOfFiles)
    {
        string methodName = nameof(WriteListOfFiles), paramList = "(listOfFiles)";

        try
        {
            using FileStream writeToStream = File.Open(_targetJSONFile, FileMode.Create);
            await JsonSerializer.SerializeAsync(writeToStream, listOfFiles);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Error in '{methodName}{paramList}'. The error is: 'Exception occurred '{ex}''.", methodName, paramList, ex);
            return Result.Fatal("An error occurred");
        }
    }
}
