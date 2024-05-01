using Azure.Storage.Blobs;
using FileArchive.Utils;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;

namespace FileArchive
{
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

		public async Task<Stream> OpenStoredFile(long id)
        {
            var blobContainerClient = new BlobContainerClient(_blobConnectionString, _blobStorageFolder);

            var blob = blobContainerClient.GetBlobClient(id.ToString());

            Stream fileStream = await blob.OpenReadAsync();

            return fileStream;
		}

        public void CloseStoredFile(Stream stream)
        {
            stream.Close();

            return;
        }

		public async Task<bool> StoreFile(long id, IBrowserFile file)
        {
            var blobContainerClient = new BlobContainerClient(_blobConnectionString, _blobStorageFolder);

            Stream myBlob = file.OpenReadStream(_maxFileSize);

            await blobContainerClient.CreateIfNotExistsAsync();

            var blob = blobContainerClient.GetBlobClient(id.ToString());

            await blob.UploadAsync(myBlob);

            return true;
        }

        public async Task DeleteStoredFile(long id)
        {
            var blobContainerClient = new BlobContainerClient(_blobConnectionString, _blobStorageFolder);

            var blob = blobContainerClient.GetBlobClient(id.ToString());
            await blob.DeleteAsync();

            return;
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
}
