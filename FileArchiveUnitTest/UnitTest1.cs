using Castle.Core.Configuration;
using FileArchive;
using FileArchive.DataAccess;
using FileArchive.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Text.Json;

namespace FileArchiveUnitTest
{
    public class Tests: IDisposable
    {
        private string _jsonTempDir = string.Empty;
        private Microsoft.Extensions.Configuration.IConfiguration _config;
        private string _jsonFilename;
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

        public static Mock<DbSet<T>> CreateDbSetMock<T>(IEnumerable<T> elements) where T : class
        {
            var elementsAsQueryable = elements.AsQueryable();
            var dbSetMock = new Mock<DbSet<T>>();

            dbSetMock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(elementsAsQueryable.Provider);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(elementsAsQueryable.Expression);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(elementsAsQueryable.ElementType);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => elementsAsQueryable.GetEnumerator());

            return dbSetMock;
        }


        [Test]
        public void CRUDJSON_CreateFileArchiveInfo()
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
            service.CreateFileInfo(fileInfo, userId);

            // Read back the file and check it:
            using FileStream createStream = File.OpenRead(_jsonFilename);
            var storedListOfFiles = JsonSerializer.Deserialize<List<FileArchiveInfo>>(createStream);
            createStream.Close();

            Assert.That(storedListOfFiles!.Count(), Is.EqualTo(1));
            Assert.That(storedListOfFiles![0].Filename, Is.EqualTo("MyFile.jpg"));
            Assert.That(storedListOfFiles![0].ParentKey, Is.EqualTo("4711"));
        }

        [Test]
        public void CRUDJSON_UpdateFileArchiveInfo()
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
            service.CreateFileInfo(fileInfo, userId);

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
            service.UpdateFileInfo(fileInfo, userId);

            // Read back the file and check it:
            using FileStream createStream = File.OpenRead(_jsonFilename);
            var storedListOfFiles = JsonSerializer.Deserialize<List<FileArchiveInfo>>(createStream);
            createStream.Close();

            Assert.That(storedListOfFiles!.Count(), Is.EqualTo(1));
            Assert.That(storedListOfFiles![0].Filename, Is.EqualTo("MyFile.jpg"));
            Assert.That(storedListOfFiles![0].Description, Is.EqualTo("Altered Description"));
            Assert.That(storedListOfFiles![0].ParentKey, Is.EqualTo("4711"));
        }

        [Test]
        public void CRUDJSON_DeleteFileArchiveInfo()
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
            service.CreateFileInfo(fileInfo, userId);

            fileInfo = new()
            {
                Filename = "File2",
                FileMimeType = "image/jpeg",
                Description = "This is a test picture",
                ParentKey = "4711"
            };

            service.CreateFileInfo(fileInfo, userId);

            fileInfo = new()
            {
                Filename = "File3",
                FileMimeType = "image/jpeg",
                Description = "This is a test picture",
                ParentKey = "4711"
            };

            service.CreateFileInfo(fileInfo, userId);

            long idToDelete = 2L;
            service.DeleteFileInfo(idToDelete);

            // Read back the file and check it:
            using FileStream createStream = File.OpenRead(_jsonFilename);
            var storedListOfFiles = JsonSerializer.Deserialize<List<FileArchiveInfo>>(createStream);
            createStream.Close();

            Assert.That(storedListOfFiles!.Count, Is.EqualTo(2));
        }


        [Test]
        public void CRUDJSON_GetForId()
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
            service.CreateFileInfo(fileInfo, userId);

            fileInfo = new()
            {
                Filename = "File2",
                FileMimeType = "image/jpeg",
                Description = "This is a test picture",
                ParentKey = "4711"
            };

            service.CreateFileInfo(fileInfo, userId);

            fileInfo = new()
            {
                Filename = "File3",
                FileMimeType = "image/jpeg",
                Description = "This is a test picture",
                ParentKey = "4711"
            };

            service.CreateFileInfo(fileInfo, userId);

            long idToRetrieve = 2L;

            var fileInfoOnId = service.GetFileInfoById(idToRetrieve);

            // Results
            var recordFound = fileInfoOnId is not null;
            Assert.That(recordFound, Is.True);
        }



        [Test]
        public void CRUDJSON_GetForParetKey()
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
            service.CreateFileInfo(fileInfo, userId);

            fileInfo = new()
            {
                Filename = "File2",
                FileMimeType = "image/jpeg",
                Description = "This is a test picture",
                ParentKey = "4712"
            };

            service.CreateFileInfo(fileInfo, userId);

            fileInfo = new()
            {
                Filename = "File3",
                FileMimeType = "image/jpeg",
                Description = "This is a test picture",
                ParentKey = "4720"
            };

            service.CreateFileInfo(fileInfo, userId);

            // Results
            string parentKey = "4712";
            var listOfFileInfos = service.GetListOfFileInfoByParentKey(parentKey);
            Assert.That(listOfFileInfos.Count, Is.EqualTo(2));

            parentKey = "4720";
            listOfFileInfos = service.GetListOfFileInfoByParentKey(parentKey);
            Assert.That(listOfFileInfos.Count, Is.EqualTo(1));

            parentKey = "4730";
            listOfFileInfos = service.GetListOfFileInfoByParentKey(parentKey);
            Assert.That(listOfFileInfos.Count, Is.EqualTo(0));
        }


        [Test]
        public void CRUDDB_CreateFileArchiveInfo()
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
            service.CreateFileInfo(fileInfo, userId);

            mockSet.Verify(m => m.Add(It.IsAny<FileArchiveInfo>()), Times.Once());
            mockContext.Verify(m => m.SaveChanges(), Times.Once());
        }


        [Test]
        public void CRUDDB_UpdateFileArchiveInfo()
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
            service.UpdateFileInfo(fileInfo, userId);

            mockSet.Verify(m => m.Update(It.IsAny<FileArchiveInfo>()), Times.Once());
            mockContext.Verify(m => m.SaveChanges(), Times.Once());
        }


        [Test]
        public void CRUDDB_DeleteFileArchiveInfo()
        {
            var data = new List<FileArchiveInfo>
            {
                new FileArchiveInfo
                {
                    Id = 10,
                    Filename = "File10"
                },
                new FileArchiveInfo
                {
                    Id = 20,
                    Filename = "File20"
                },
                new FileArchiveInfo
                {
                    Id = 30,
                    Filename = "File30"
                }
            };

            var mockDbSet = CreateDbSetMock<FileArchiveInfo>(data);
            var mockContext = new Mock<IFileArchiveContext>();

            mockDbSet.Setup(m => m.Remove(It.IsAny<FileArchiveInfo>())).Callback<FileArchiveInfo>((entity) => data.Remove(entity));

            mockContext.Setup(x => x.FileArchiveInfos).Returns(mockDbSet.Object);

            var service = new FileArchiveFileInfoCRUDDB(mockContext.Object);
            long idToDelete = 20L;
            service.DeleteFileInfo(idToDelete);

            // Results
            mockContext.VerifyGet(x => x.FileArchiveInfos, Times.Exactly(2));
            mockContext.Verify(x => x.SaveChanges(), Times.Once());

            Assert.That(data.Count, Is.EqualTo(2));
        }


        [Test]
        public void CRUDDB_GetForId()
        {
            var data = new List<FileArchiveInfo>
            {
                new FileArchiveInfo
                {
                    Id = 10,
                    Filename = "File10",
                    ParentKey = "4712"
                },
                new FileArchiveInfo
                {
                    Id = 20,
                    Filename = "File20",
                    ParentKey = "4712"
                },
                new FileArchiveInfo
                {
                    Id = 30,
                    Filename = "File30",
                    ParentKey = "4720"
                }
            };

            var mockDbSet = CreateDbSetMock<FileArchiveInfo>(data);
            var mockContext = new Mock<IFileArchiveContext>();

            mockContext.Setup(x => x.FileArchiveInfos).Returns(mockDbSet.Object);

            var service = new FileArchiveFileInfoCRUDDB(mockContext.Object);

            long idToRetrieve = 20L;

            var fileInfo = service.GetFileInfoById(idToRetrieve);

            // Results
            mockContext.VerifyGet(x => x.FileArchiveInfos, Times.Exactly(1));

            var recordFound = fileInfo is not null;
            Assert.That(recordFound, Is.True);
        }


        [Test]
        public void CRUDDB_GetForParetKey()
        {
            var data = new List<FileArchiveInfo>
            {
                new FileArchiveInfo
                {
                    Id = 10,
                    Filename = "File10",
                    ParentKey = "4712"
                },
                new FileArchiveInfo
                {
                    Id = 20,
                    Filename = "File20",
                    ParentKey = "4712"
                },
                new FileArchiveInfo
                {
                    Id = 30,
                    Filename = "File30",
                    ParentKey = "4720"
                }
            };

            var mockDbSet = CreateDbSetMock<FileArchiveInfo>(data);
            var mockContext = new Mock<IFileArchiveContext>();

            mockContext.Setup(x => x.FileArchiveInfos).Returns(mockDbSet.Object);

            var service = new FileArchiveFileInfoCRUDDB(mockContext.Object);

            string parentKey = "4712";
            var listOfFileInfos = service.GetListOfFileInfoByParentKey(parentKey);

            // Results
            mockContext.VerifyGet(x => x.FileArchiveInfos, Times.Exactly(1));
            Assert.That(listOfFileInfos.Count, Is.EqualTo(2));

            parentKey = "4720";
            listOfFileInfos = service.GetListOfFileInfoByParentKey(parentKey);
            Assert.That(listOfFileInfos.Count, Is.EqualTo(1));

            parentKey = "4730";
            listOfFileInfos = service.GetListOfFileInfoByParentKey(parentKey);
            Assert.That(listOfFileInfos.Count, Is.EqualTo(0));
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
}