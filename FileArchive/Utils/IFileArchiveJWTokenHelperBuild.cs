using static FileArchive.Utils.FileArchiveJWTokenHelperRead;

namespace FileArchive.Utils;

public interface IFileArchiveJWTokenHelperBuild
{
    ResultObject<string> BuildTokenForFileDownload(string curUserId, long id);
}
