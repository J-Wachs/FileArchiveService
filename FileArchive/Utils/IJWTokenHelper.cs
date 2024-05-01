using System.Security.Claims;

namespace FileArchive.Utils;

public interface IJWTokenHelper
{
    ResultObject<string> GenerateToken(string userId, IDictionary<string, string> claims, int expireMinutes = 60);
    ResultObject<ClaimsPrincipal> ValidateToken(string jwToken);
}
