using System.Security.Claims;

namespace FileArchive.Utils;

public interface IJWTokenHelper
{
    Result<string> GenerateToken(string userId, IDictionary<string, string> claims, int expireMinutes = 60);
    Result<ClaimsPrincipal> ValidateToken(string jwToken);
}
