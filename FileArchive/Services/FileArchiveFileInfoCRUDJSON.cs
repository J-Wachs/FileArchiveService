using FileArchive.Interfaces;
using FileArchive.Models;
using FileArchive.Utils;
using Microsoft.Extensions.Configuration;
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

    private const int NotFound = -1;

    public FileArchiveFileInfoCRUDJSON(IConfiguration config)
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
            return Result.Failure(readListResult.Messages);
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
        string methodName = $"{nameof(UpdateFileInfo)}", paramList = $"(fileInfo, '{userId}')";
        
        Result<List<FileArchiveInfo>?> readListOfFilesResult = await ReadListOfFiles();
        if (readListOfFilesResult.IsSuccess is false)
        {
            return Result.Failure(readListOfFilesResult.Messages);
        }
        var listOfFiles = readListOfFilesResult.Data!;

        var index = listOfFiles.FindIndex(x => x.Id == fileInfo.Id);
        if (index is NotFound)
        {
            return Result.Failure($"Error occurred in '{methodName}', The error is: 'The File Id is not found in JSON file'.");
        }

        fileInfo.LastModified = DateTime.Now;
        fileInfo.LastModifiedBy = userId;
        listOfFiles[index] = fileInfo;

        Result writeListResult = await WriteListOfFiles(listOfFiles);

        return writeListResult;
    }


    public async ValueTask<Result> DeleteFileInfo(long id)
    {
        var readListResult = await ReadListOfFiles();
        if (readListResult.IsSuccess is false)
        {
            return Result.Failure(readListResult.Messages);
        }

        var listOfFiles = readListResult.Data!.Where(x => x.Id != id).ToList();
        Result writeListOfFilesResult = await WriteListOfFiles(listOfFiles);
        return writeListOfFilesResult;
    }

    public async ValueTask<Result<IList<FileArchiveInfo>?>> GetListOfFileInfoByParentKey(string parentKey)
    {
        var readListResult = await ReadListOfFiles();
        if (readListResult.IsSuccess is false)
        {
            return Result<IList<FileArchiveInfo>?>.Failure(readListResult.Messages);
        }
        var listOfFiles = readListResult.Data!;

        return Result<IList<FileArchiveInfo>?>.Success([.. listOfFiles.Where(x => x.ParentKey == parentKey)]);
    }

    public async ValueTask<Result<FileArchiveInfo?>> GetFileInfoById(long id)
    {
        var readListResult = await ReadListOfFiles();
        if (readListResult.IsSuccess is false)
        {
            return Result<FileArchiveInfo?>.Failure(readListResult.Messages);

        }

        return Result<FileArchiveInfo?>.Success(readListResult.Data!.FirstOrDefault(x => x.Id == id));
    }

    private async ValueTask<Result<List<FileArchiveInfo>?>> ReadListOfFiles()
    {
        string methodName = $"{nameof(ReadListOfFiles)}", paramList = "()";

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
            return Result<List<FileArchiveInfo>?>.Failure($"Error occurred in '{methodName}{paramList}', The error is: '{ex}'.");
        }
    }

    private async ValueTask<Result> WriteListOfFiles(IList<FileArchiveInfo> listOfFiles)
    {
        string methodName = $"{nameof(WriteListOfFiles)}", paramList = "(listOfFiles)";

        try
        {
            using FileStream writeToStream = File.Open(_targetJSONFile, FileMode.Create);
            await JsonSerializer.SerializeAsync(writeToStream, listOfFiles);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error occurred in '{methodName}{paramList}', The error is: '{ex}'.");
        }
    }
}
