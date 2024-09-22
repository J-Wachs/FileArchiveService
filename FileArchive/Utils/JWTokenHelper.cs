using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FileArchive.Utils;

/// <summary>
/// Class that provides methods to build and validate Java Web Tokens.
/// </summary>
/// <param name="config"></param>
public class JWTokenHelper(IConfiguration config) : IJWTokenHelper
{
    private const string JWTIssuer = "Jwt:Issuer";
    private const string JWTAudience = "Jwt:Audience";
    private const string JWTSecret = "Jwt:Secret";

    public const string JWTClaimSubject = "sub";
    private readonly string _issuer = ConfigHelper.GetMustExistConfigValue<string>(config, JWTIssuer);
    private readonly string _audience = ConfigHelper.GetMustExistConfigValue<string>(config, JWTAudience);
    private readonly string _secret = ConfigHelper.GetMustExistConfigValue<string>(config, JWTSecret);

    public Result<ClaimsPrincipal> ValidateToken(string jwToken)
    {
        if (String.IsNullOrWhiteSpace(jwToken))
        {
            return Result<ClaimsPrincipal>.Failure($"{nameof(ValidateToken)}: jwToken is missing");
        }

        var resultPrincipal = GetPrincipal(jwToken);
        if (resultPrincipal.IsSuccess is false)
        {
            return Result<ClaimsPrincipal>.Failure(resultPrincipal.Messages);
        }

        if (resultPrincipal.Data is null || resultPrincipal.Data.Claims.Any() is false)
        {
            return Result<ClaimsPrincipal>.Failure($"{nameof(ValidateToken)}: There is no Claims in the token");
        }

        if (resultPrincipal.Data.Identity?.IsAuthenticated is false)
        {
            return Result<ClaimsPrincipal>.Failure($"{nameof(ValidateToken)}: The user is not identified in the calling client.");
        }

        return Result<ClaimsPrincipal>.Success(resultPrincipal.Data);
    }

    private Result<ClaimsPrincipal> GetPrincipal(string token)
    {
        try
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));

            var tokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken? jwToken = null;

            if (tokenHandler.ReadToken(token) is JwtSecurityToken workJWToken)
            {
                jwToken = workJWToken;
            }
            
            if (jwToken is null)
            {
                return Result<ClaimsPrincipal>.Failure("Token cannot be read");
            }

            var validationParameters = new TokenValidationParameters()
            {
                RequireExpirationTime = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                IssuerSigningKey = securityKey,
                ValidIssuer = _issuer,
                ValidAudience = _audience
            };

            // The Tokenhandler convert by default between short and long name of claims. Disable it.
            tokenHandler.InboundClaimTypeMap.Clear();
            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken securityToken);

            return Result<ClaimsPrincipal>.Success(principal);
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            return Result<ClaimsPrincipal>.Failure($"{nameof(GetPrincipal)}: Signature does not match: '{ex}'");
        }
        catch (Exception ex)
        {
            return Result<ClaimsPrincipal>.Failure($"{nameof(GetPrincipal)}: An exception has occurred: '{ex}'");
        }
    }

    public Result<string> GenerateToken(string userId, IDictionary<string, string> claims, int expireMinutes = 60)
    {
        if (String.IsNullOrWhiteSpace(userId))
        {
            return Result<string>.Failure($"{nameof(GenerateToken)}: User Id must be supplied");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var tokenHandler = new JwtSecurityTokenHandler();

        var now = DateTime.UtcNow;

        ClaimsIdentity claimsIdentity = new();

        claimsIdentity.AddClaim(new Claim(JWTClaimSubject, userId));

        foreach (var claim in claims)
        {
            claimsIdentity.AddClaim(new(claim.Key, claim.Value));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _issuer,
            Audience = _audience,
            Subject = claimsIdentity,
            Expires = now.AddMinutes(Convert.ToInt32(expireMinutes)),
            SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var stoken = tokenHandler.CreateToken(tokenDescriptor);
        var token = tokenHandler.WriteToken(stoken);

        return Result<string>.Success(token);
    }
}
