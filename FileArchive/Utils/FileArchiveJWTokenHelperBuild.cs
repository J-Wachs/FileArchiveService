namespace FileArchive.Utils;

public class FileArchiveJWTokenHelperBuild(IJWTokenHelper jwTokenHelper) : IFileArchiveJWTokenHelperBuild
{
    private const string JWTFileId = "file_id";

    public ResultObject<string> BuildTokenForFileDownload(string curUserId, long id)
    {
        IDictionary<string, string> claims = new Dictionary<string, string>
        {
            { JWTFileId, id.ToString() }
        };

        return jwTokenHelper.GenerateToken(curUserId, claims);
    }
}
