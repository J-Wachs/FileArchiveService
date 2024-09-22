using FileArchive.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

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

    public void CreateFileInfo(FileArchiveInfo fileInfo, string userId)
    {
        fileInfo.Created = DateTime.Now;
        fileInfo.CreatedBy = userId;
        fileInfo.LastModified = DateTime.Now;
        fileInfo.LastModifiedBy = userId;

        var listOfFiles = ReadListOfFiles();

        // Not that clever way to get next free fileId:
        long fileId = 0;
        var newFileId = listOfFiles.MaxBy(x => x.Id)?.Id;
        newFileId ??= 0;
        fileId = (long)newFileId + 1;

        fileInfo.Id = fileId;
        listOfFiles.Add(fileInfo);

        WriteListOfFiles(listOfFiles);
    }

    public void UpdateFileInfo(FileArchiveInfo fileInfo, string userId)
    {
        List<FileArchiveInfo> listOfFiles = ReadListOfFiles();
        var index = listOfFiles.FindIndex(x => x.Id == fileInfo.Id);
        if (index is NotFound)
        {
            throw new Exception("The File Id is not found in JSON file");
        }

        fileInfo.LastModified = DateTime.Now;
        fileInfo.LastModifiedBy = userId;
        listOfFiles[index] = fileInfo;
        WriteListOfFiles(listOfFiles);
    }


    public void DeleteFileInfo(long id)
    {
        var listOfFiles = ReadListOfFiles().Where(x => x.Id != id).ToList();
        WriteListOfFiles(listOfFiles);
    }

    public IList<FileArchiveInfo> GetListOfFileInfoByParentKey(string parentKey)
    {
        return ReadListOfFiles().Where(x => x.ParentKey == parentKey).ToList();
    }

    public FileArchiveInfo? GetFileInfoById(long id)
    {
        return ReadListOfFiles().FirstOrDefault(x => x.Id == id);
    }

    private List<FileArchiveInfo> ReadListOfFiles()
    {
        List<FileArchiveInfo>? listOfFiles = null;

        if (File.Exists(_targetJSONFile))
        {
            using FileStream createStream = File.OpenRead(_targetJSONFile);
            listOfFiles = JsonSerializer.Deserialize<List<FileArchiveInfo>>(createStream);
            createStream.Close();
        }

        listOfFiles ??= [];

        return listOfFiles;
    }

    private void WriteListOfFiles(IList<FileArchiveInfo> listOfFiles)
    {
        using FileStream writeToStream = File.Open(_targetJSONFile, FileMode.Create);
        JsonSerializer.Serialize(writeToStream, listOfFiles);
    }
}
