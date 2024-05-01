namespace FileArchive.Utils;

public class FileArchiveJWTokenHelperRead(IJWTokenHelper jwTokenHelper) : IFileArchiveJWTokenHelperRead
{
    // Claim name for the id of a file.
    private const string JWTFileId = "file_id";

    public record UserIdAndFileIdDTO(long UserId, long FileId);


    public UserIdAndFileIdDTO GetUserIdAndFileIdFromJWToken(string jwToken)
    {
        var result = jwTokenHelper.ValidateToken(jwToken);
        if (result.Success is false)
        {
            throw new InvalidOperationException();
        }

        var temp = result.Result!.Claims.First(x => x.Type == JWTokenHelper.JWTClaimSubject).Value;
        long userId = long.Parse(temp);

        temp = result.Result.Claims.First(x => x.Type == JWTFileId).Value;
        long fileId = long.Parse(temp);

        return new UserIdAndFileIdDTO(userId, fileId);
    }
}
