using Microsoft.AspNetCore.Components.Forms;

namespace FileArchive;

public interface IFileArchiveStorage
{
    Task<bool> StoreFile(long id, IBrowserFile file);
    Task<Stream> OpenStoredFile(long id);
    void CloseStoredFile(Stream stream);
    Task DeleteStoredFile(long id);
    void SetMaxFileSize(long maxFileSize);
}
