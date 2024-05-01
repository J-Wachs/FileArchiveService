using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FileArchive.Models;

/// <summary>
/// Class that represent the data stored about each file in the File Archive.
/// </summary>
[Index(nameof(ParentKey), nameof(Filename), Name = "IX_ParentKey_Filename")]
public class FileArchiveInfo
{
    public long Id { get; set; }
    [Required]
    [MaxLength(250)]
    public string Filename { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? FileMimeType { get; set; }

    [MaxLength(250)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? ParentKey { get; set; }
    public DateTime Created { get; set; }

    [MaxLength(50)]
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? LastModified { get; set; }

    [MaxLength(50)]
    public string? LastModifiedBy { get; set; }
}