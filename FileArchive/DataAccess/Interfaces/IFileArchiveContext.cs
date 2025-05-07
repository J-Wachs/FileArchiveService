using FileArchive.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FileArchive.DataAccess.Interfaces;

/// <summary>
/// Db context for this File Archive service project
/// </summary>
public interface IFileArchiveContext
{
    /// <summary>
    /// Information about the files in the File Archive.
    /// </summary>
    DbSet<FileArchiveInfo> FileArchiveInfos { get; set; }

    EntityEntry Entry(object entity);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
