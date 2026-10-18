namespace Gateway.Api.Services
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
 
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Tokens;

    using Gateway.Api.Configuration;

    public sealed class TokenService(IOptions<JwtOptions> options) : ITokenService
    {
        private readonly JwtOptions _options = options.Value;

        public string CreateAccessToken(string subjectId, string email, string scope)
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, subjectId),
            new Claim(ClaimTypes.Email, email),
            new Claim("scope", scope)
        };

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
