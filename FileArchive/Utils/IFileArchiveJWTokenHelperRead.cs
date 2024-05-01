using static FileArchive.Utils.FileArchiveJWTokenHelperRead;

namespace FileArchive.Utils;

public interface IFileArchiveJWTokenHelperRead
{
    UserIdAndFileIdDTO GetUserIdAndFileIdFromJWToken(string jwToken);
}
