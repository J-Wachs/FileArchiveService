using FileArchive.Models;
using Microsoft.EntityFrameworkCore;

namespace FileArchive.DataAccess;

/// <summary>
/// Demo dbContext for this File Archive service demo project
/// </summary>
public class FileArchiveContext(DbContextOptions options) : DbContext(options), IFileArchiveContext
{
    public DbSet<FileArchiveInfo> FileArchiveInfos { get; set; }
}
