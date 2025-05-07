using Microsoft.AspNetCore.Components.Forms;

namespace FileArchive.Models;

/// <summary>
/// File information to be exchanged between the UI component and the file archive service.
/// </summary>
public class FileArchiveFileInfoUI
{
    public long? Id { get; set; }
    public string Filename { get; set; } = string.Empty;
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
