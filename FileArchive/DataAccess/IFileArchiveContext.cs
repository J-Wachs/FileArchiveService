using FileArchive.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FileArchive.DataAccess;

/// <summary>
/// Demo dbContext for this File Archive service demo project
/// </summary>
public interface IFileArchiveContext
{
    /// <summary>
    /// Information about the files in the File Archive.
    /// </summary>
    DbSet<FileArchiveInfo> FileArchiveInfos { get; set; }

    EntityEntry Entry(object entity);

    int SaveChanges();
}
