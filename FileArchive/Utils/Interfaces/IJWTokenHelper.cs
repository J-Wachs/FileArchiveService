using System.Security.Claims;

namespace FileArchive.Utils.Interfaces;

/// <summary>
/// Interface for helpers to manage Json Web Token.
/// </summary>
public interface IJWTokenHelper
{
    /// <summary>
    /// Generate a Json Web Token.
    /// </summary>
    /// <param name="userId">Id of the user</param>
    /// <param name="claims">Claims to include in the token</param>
    /// <param name="expireMinutes">Time in minutes for the token to expire</param>
    /// <returns></returns>
    Result<string> GenerateToken(string userId, IDictionary<string, string> claims, int expireMinutes = 60);

    /// <summary>
    /// Validate the token and it's content.
    /// </summary>
    /// <param name="jwToken">The Json Web Token to validate</param>
    /// <returns>The claims in the token</returns>
    Result<ClaimsPrincipal> ValidateToken(string jwToken);
}
