using FileArchive.Utils.Interfaces;

namespace FileArchive.Utils;

public class FileArchiveJWTokenHelperRead(IJWTokenHelper jwTokenHelper) : IFileArchiveJWTokenHelperRead
{
    // Claim name for the id of a file.
    private const string JWTFileId = "file_id";

    public record UserIdAndFileIdDTO(long UserId, long FileId);

    public Result<UserIdAndFileIdDTO?> GetUserIdAndFileIdFromJWToken(string jwToken)
    {
        var result = jwTokenHelper.ValidateToken(jwToken);
        if (result.IsSuccess is false)
        {
            return Result<UserIdAndFileIdDTO?>.Failure(result.Messages);
        }

        var claim = result.Data!.Claims.First(x => x.Type == JWTokenHelper.JWTClaimSubject).Value;
        long userId = long.Parse(claim);

        claim = result.Data.Claims.First(x => x.Type == JWTFileId).Value;
        long fileId = long.Parse(claim);

        return Result<UserIdAndFileIdDTO?>.Success(new UserIdAndFileIdDTO(userId, fileId));
    }
}
