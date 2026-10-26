namespace Gateway.IntegrationTests;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;

public static class TestJwtFactory
{
    private const string Key = "ReplaceWithYourOwnRandomSecretAtLeast32BytesLong";
    private const string Issuer = "mcp-gateway";
    private const string Audience = "mcp-gateway";

    public static string CreateToken(string subjectId, string email, string[] scopes)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, subjectId),
            new(ClaimTypes.Email, email),
            new("scope", string.Join(" ", scopes))
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
