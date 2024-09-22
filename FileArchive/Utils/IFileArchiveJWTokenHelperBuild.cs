namespace FileArchive.Utils;

public interface IFileArchiveJWTokenHelperBuild
{
    Result<string> BuildTokenForFileDownload(string curUserId, long id);
}
