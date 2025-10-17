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
    /// <summary>
    /// Is file to be updated?
    /// </summary>
    public bool Update { get; set; }
    /// <summary>
    /// Is file to be deleted?
    /// </summary>
    public bool Delete { get; set; }
    /// <summary>
    /// Is file to be inserted?
    /// </summary>
    public bool Insert { get; set; }
    /// <summary>
    /// Information about the actual file.
    /// </summary>
    public IBrowserFile? File { get; set; }
}
