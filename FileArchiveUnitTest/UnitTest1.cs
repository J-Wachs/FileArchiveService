using FileArchive.DataAccess.Interfaces;
using FileArchive.Models;
using FileArchive.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Text.Json;
using YourNamespace.TestUtils;

namespace FileArchiveUnitTest;

public class Tests: IDisposable
{
    private readonly string _jsonTempDir = string.Empty;
    private readonly IConfiguration _config;
    private readonly string _jsonFilename;
    private bool _IsDisposed;

    public Tests()
    {
        // Make a temp folder for holding the FileInfo.json file to be used.
        var result = Directory.CreateTempSubdirectory();
        _jsonTempDir = result.FullName;
        _jsonFilename = Path.Combine(_jsonTempDir, "FileInfo.json");

        Dictionary<string, string?> inMemoryConfig = new()
        {
            { FileArchiveConstants.ConfigPath, result.FullName }
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();
    }


    [SetUp]
    public void Setup()
    {
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_jsonFilename))
        {
            File.Delete(_jsonFilename);
        }
    }

    [Test]
    public async Task CRUDJSON_CreateFileArchiveInfo()
    {
        var service = new FileArchiveFileInfoCRUDJSON(_config);

        FileArchiveInfo fileInfo = new()
        {
            Id = 47,
            Filename = "MyFile.jpg",
            FileMimeType = "image/jpeg",
            Description = "This is a test picture",
            ParentKey = "4711"
        };

        string userId = "8888-9999";
        _ = await service.CreateFileInfo(fileInfo, userId);

        // Read back the file and check it:
        using FileStream createStream = File.OpenRead(_jsonFilename);
        var storedListOfFiles = JsonSerializer.Deserialize<List<FileArchiveInfo>>(createStream);
        createStream.Close();

        Assert.That(storedListOfFiles!.Count, Is.EqualTo(1));
        Assert.That(storedListOfFiles![0].Filename, Is.EqualTo("MyFile.jpg"));
        Assert.That(storedListOfFiles![0].ParentKey, Is.EqualTo("4711"));
    }

    [Test]
    public async Task CRUDJSON_UpdateFileArchiveInfo()
    {
        var service = new FileArchiveFileInfoCRUDJSON(_config);

        FileArchiveInfo fileInfo = new()
        {
            Id = 47,
            Filename = "MyFile.jpg",
            FileMimeType = "image/jpeg",
            Description = "This is a test picture",
            ParentKey = "4711"
        };

        string userId = "8888-9999";
        _ = await service.CreateFileInfo(fileInfo, userId);

        var storedId = fileInfo.Id;

        fileInfo = new()
        {
            Id = storedId,
            Filename = "MyFile.jpg",
            FileMimeType = "image/jpeg",
            Description = "Altered Description",
            ParentKey = "4711"
        };

        userId = "8888-9999";
        _ = await service.UpdateFileInfo(fileInfo, userId);

        // Read back the file and check it:
        using FileStream createStream = File.OpenRead(_jsonFilename);
        var storedListOfFiles = JsonSerializer.Deserialize<List<FileArchiveInfo>>(createStream);
        createStream.Close();

        Assert.That(storedListOfFiles!.Count, Is.EqualTo(1));
        Assert.That(storedListOfFiles![0].Filename, Is.EqualTo("MyFile.jpg"));
        Assert.That(storedListOfFiles![0].Description, Is.EqualTo("Altered Description"));
        Assert.That(storedListOfFiles![0].ParentKey, Is.EqualTo("4711"));
    }

    [Test]
    public async Task CRUDJSON_DeleteFileArchiveInfo()
    {
        var service = new FileArchiveFileInfoCRUDJSON(_config);

        FileArchiveInfo fileInfo = new()
        {
            Filename = "File1",
            FileMimeType = "image/jpeg",
            Description = "This is a test picture",
            ParentKey = "4711"
        };

        string userId = "8888-9999";
        _ = await service.CreateFileInfo(fileInfo, userId);

        fileInfo = new()
        {
            Filename = "File2",
            FileMimeType = "image/jpeg",
            Description = "This is a test picture",
            ParentKey = "4711"
        };

        _ = await service.CreateFileInfo(fileInfo, userId);

        fileInfo = new()
        {
            Filename = "File3",
            FileMimeType = "image/jpeg",
            Description = "This is a test picture",
            ParentKey = "4711"
        };

        _ = await service.CreateFileInfo(fileInfo, userId);

        long idToDelete = 2L;
        _ = await service.DeleteFileInfo(idToDelete);

        // Read back the file and check it:
        using FileStream createStream = File.OpenRead(_jsonFilename);
        var storedListOfFiles = JsonSerializer.Deserialize<List<FileArchiveInfo>>(createStream);
        createStream.Close();

        Assert.That(storedListOfFiles!.Count, Is.EqualTo(2));
    }


    [Test]
    public async Task CRUDJSON_GetForId()
    {
        var service = new FileArchiveFileInfoCRUDJSON(_config);

        FileArchiveInfo fileInfo = new()
        {
            Filename = "File1",
            FileMimeType = "image/jpeg",
            Description = "This is a test picture",
            ParentKey = "4711"
        };

        string userId = "8888-9999";
        _ = await service.CreateFileInfo(fileInfo, userId);

        fileInfo = new()
        {
            Filename = "File2",
            FileMimeType = "image/jpeg",
            Description = "This is a test picture",
            ParentKey = "4711"
        };

        _ = await service.CreateFileInfo(fileInfo, userId);

        fileInfo = new()
        {
            Filename = "File3",
            FileMimeType = "image/jpeg",
            Description = "This is a test picture",
            ParentKey = "4711"
        };

        _ = await service.CreateFileInfo(fileInfo, userId);

        long idToRetrieve = 2L;

        var fileInfoOnId = await service.GetFileInfoById(idToRetrieve);

        // Results
        Assert.That(fileInfoOnId.IsSuccess, Is.True);
    }



    [Test]
    public async Task CRUDJSON_GetForParetKey()
    {
        var service = new FileArchiveFileInfoCRUDJSON(_config);

        FileArchiveInfo fileInfo = new()
        {
            Filename = "File1",
            FileMimeType = "image/jpeg",
            Description = "This is a test picture",
            ParentKey = "4712"
        };

        string userId = "8888-9999";
        _ = await service.CreateFileInfo(fileInfo, userId);

        fileInfo = new()
        {
            Filename = "File2",
            FileMimeType = "image/jpeg",
            Description = "This is a test picture",
            ParentKey = "4712"
        };

        _ = await service.CreateFileInfo(fileInfo, userId);

        fileInfo = new()
        {
            Filename = "File3",
            FileMimeType = "image/jpeg",
            Description = "This is a test picture",
            ParentKey = "4720"
        };

        _  = await service.CreateFileInfo(fileInfo, userId);

        // Results
        string parentKey = "4712";
        var listOfFileInfos = await service.GetListOfFileInfoByParentKey(parentKey);
        Assert.That(listOfFileInfos.Data!.Count, Is.EqualTo(2));

        parentKey = "4720";
        listOfFileInfos = await service.GetListOfFileInfoByParentKey(parentKey);
        Assert.That(listOfFileInfos.Data!.Count, Is.EqualTo(1));

        parentKey = "4730";
        listOfFileInfos = await service.GetListOfFileInfoByParentKey(parentKey);
        Assert.That(listOfFileInfos.Data!.Count, Is.EqualTo(0));
    }


    [Test]
    public async Task CRUDDB_CreateFileArchiveInfo()
    {
        var mockSet = new Mock<DbSet<FileArchiveInfo>>();

        var mockContext = new Mock<IFileArchiveContext>();
        mockContext.Setup(x => x.FileArchiveInfos).Returns(mockSet.Object);

        var service = new FileArchiveFileInfoCRUDDB(mockContext.Object);

        FileArchiveInfo fileInfo = new()
        {
            Id = 47,
            Filename = "MyFile.jpg",
            FileMimeType = "image/jpeg",
            Description = "This is a test picture",
            ParentKey = "4711"
        };

        string userId = "8888-9999";
        await service.CreateFileInfo(fileInfo, userId);

        mockSet.Verify(m => m.Add(It.IsAny<FileArchiveInfo>()), Times.Once());
    }


    [Test]
    public async Task CRUDDB_UpdateFileArchiveInfo()
    {
        var mockSet = new Mock<DbSet<FileArchiveInfo>>();

        var mockContext = new Mock<IFileArchiveContext>();
        mockContext.Setup(x => x.FileArchiveInfos).Returns(mockSet.Object);

        var service = new FileArchiveFileInfoCRUDDB(mockContext.Object);

        FileArchiveInfo fileInfo = new()
        {
            Id = 47,
            Filename = "MyFile.jpg",
            FileMimeType = "image/jpeg",
            Description = "This is a test picture",
            ParentKey = "4711"
        };

        string userId = "8888-9999";
        await service.UpdateFileInfo(fileInfo, userId);

        mockSet.Verify(m => m.Update(It.IsAny<FileArchiveInfo>()), Times.Once());
    }


    [Test]
    public async Task CRUDDB_DeleteFileArchiveInfo()
    {
        var data = new List<FileArchiveInfo>
        {
            new() {
                Id = 10,
                Filename = "File10"
            },
            new() {
                Id = 20,
                Filename = "File20"
            },
            new() {
                Id = 30,
                Filename = "File30"
            }
        };

        var mockContext = new Mock<IFileArchiveContext>();

        var mockSet = MockDbSetHelper.CreateMockDbSet(data);
        mockContext.Setup(c => c.FileArchiveInfos).Returns(mockSet.Object);

        var service = new FileArchiveFileInfoCRUDDB(mockContext.Object);
        long idToDelete = 20L;
        var result = await service.DeleteFileInfo(idToDelete);

        // Results
        mockContext.VerifyGet(x => x.FileArchiveInfos, Times.Exactly(2));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(data.Count, Is.EqualTo(2));
    }


    [Test]
    public async Task CRUDDB_GetForId()
    {
        var data = new List<FileArchiveInfo>
        {
            new() {
                Id = 10,
                Filename = "File10",
                ParentKey = "4712"
            },
            new() {
                Id = 20,
                Filename = "File20",
                ParentKey = "4712"
            },
            new() {
                Id = 30,
                Filename = "File30",
                ParentKey = "4720"
            }
        };

        var mockContext = new Mock<IFileArchiveContext>();

        var mockSet = MockDbSetHelper.CreateMockDbSet(data);
        mockContext.Setup(c => c.FileArchiveInfos).Returns(mockSet.Object);

        var service = new FileArchiveFileInfoCRUDDB(mockContext.Object);

        long idToRetrieve = 20L;

        var fileInfoResult = await service.GetFileInfoById(idToRetrieve);

        // Results
        mockContext.VerifyGet(x => x.FileArchiveInfos, Times.Exactly(1));

        var recordFound = fileInfoResult.Data is not null;
        Assert.That(recordFound, Is.True);
        Assert.That(fileInfoResult.Data!.Id, Is.EqualTo(20L));
    }


    [Test]
    public async Task CRUDDB_GetForParetKey()
    {
        var data = new List<FileArchiveInfo>
        {
            new() {
                Id = 10,
                Filename = "File10",
                ParentKey = "4712"
            },
            new() {
                Id = 20,
                Filename = "File20",
                ParentKey = "4712"
            },
            new() {
                Id = 30,
                Filename = "File30",
                ParentKey = "4720"
            }
        };

        var mockContext = new Mock<IFileArchiveContext>();

        var mockSet = MockDbSetHelper.CreateMockDbSet(data);
        mockContext.Setup(c => c.FileArchiveInfos).Returns(mockSet.Object);

        var service = new FileArchiveFileInfoCRUDDB(mockContext.Object);

        string parentKey = "4712";
        var listOfFileInfos = await service.GetListOfFileInfoByParentKey(parentKey);

        // Results
        mockContext.VerifyGet(x => x.FileArchiveInfos, Times.Exactly(1));
        Assert.That(listOfFileInfos.Data!.Count, Is.EqualTo(2));

        parentKey = "4720";
        listOfFileInfos = await service.GetListOfFileInfoByParentKey(parentKey);
        Assert.That(listOfFileInfos.Data!.Count, Is.EqualTo(1));

        parentKey = "4730";
        listOfFileInfos = await service.GetListOfFileInfoByParentKey(parentKey);
        Assert.That(listOfFileInfos.Data!.Count, Is.EqualTo(0));
    }


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        try
        {
            if (_IsDisposed is false)
            {
                // if (disposing)
                // {
                // }

                if (Directory.Exists(_jsonTempDir))
                {
                    Directory.Delete(_jsonTempDir);
                }
            }
        }
        finally
        {
            _IsDisposed = true;
        }
    }

    ~Tests() => Dispose(false);
}