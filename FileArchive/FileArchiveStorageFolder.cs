using FileArchive.Utils;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;

namespace FileArchive
{
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

		public async Task<Stream> OpenStoredFile(long id)
        {
			if (_targetPath is not null)
			{
				var filePath = Path.Combine(_targetPath, id.ToString());

				FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                await Task.CompletedTask;
                return fileStream;
			}
			throw new ArgumentNullException(FileArchiveConstants.ConfigPath);
		}

        public void CloseStoredFile(Stream stream)
        {
            stream.Close();

            return;
        }

		public async Task<bool> StoreFile(long id, IBrowserFile file)
        {
            // Her skal der noget Try-Catch rundt om, da der kan opstå en fejl hvis
            // filen er større end det tilladte.

            if (_targetPath is not null)
            {
                var filePath = Path.Combine(_targetPath, id.ToString());

                await using FileStream output = new(filePath, FileMode.Create);
                await file.OpenReadStream(_maxFileSize).CopyToAsync(output);

                return true;
            }
            throw new ArgumentNullException(FileArchiveConstants.ConfigPath);
        }

        public async Task DeleteStoredFile(long id)
        {
            await Task.Run(() =>
            {
                if (_targetPath is not null)
                {
					var filePath = Path.Combine(_targetPath, id.ToString());
					File.Delete(filePath);
				}
			});

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
