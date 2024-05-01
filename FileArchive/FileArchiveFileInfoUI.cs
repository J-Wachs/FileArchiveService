using Microsoft.AspNetCore.Components.Forms;

namespace FileArchive;

public class FileArchiveFileInfoUI
{
    public long? Id { get; set; }
    public string Filename { get; set; } = String.Empty;
    public string? Description { get;set; }
    public string? ParentKey { get; set; }
    public DateTime? Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }

    public bool Update { get; set; }
    public bool Delete { get; set; }
    public bool Insert { get; set; }
    public IBrowserFile? File { get; set; }
}
