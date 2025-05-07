using FileArchive.Utils.Interfaces;

namespace FileArchive.Utils;

public class FileArchiveJWTokenHelperBuild(IJWTokenHelper jwTokenHelper) : IFileArchiveJWTokenHelperBuild
{
    private const string JWTFileId = "file_id";

    public Result<string> BuildTokenForFileDownload(string userId, long fileId)
    {
        IDictionary<string, string> claims = new Dictionary<string, string>
        {
            { JWTFileId, fileId.ToString() }
        };

        return jwTokenHelper.GenerateToken(userId, claims);
    }
}
