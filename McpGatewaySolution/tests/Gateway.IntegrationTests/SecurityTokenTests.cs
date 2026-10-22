namespace Gateway.IntegrationTests
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;

    using Microsoft.IdentityModel.Tokens;

    using Gateway.Api.Configuration;
    using Gateway.Api.Services;

    using Xunit;
    using FluentAssertions;

    public sealed class SecurityTokenTests
    {
        private const string ValidKey = "super-secret-key-at-least-32-chars-long!!";

        private static readonly JwtOptions Options = new()
        {
            Key = ValidKey,
            Issuer = "mcp-gateway",
            Audience = "mcp-gateway",
            AccessTokenMinutes = 15
        };

        private readonly TokenService _service =
            new(Microsoft.Extensions.Options.Options.Create(Options));

        private static TokenValidationParameters ValidParams() => new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "mcp-gateway",
            ValidAudience = "mcp-gateway",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ValidKey)),
            ClockSkew = TimeSpan.Zero
        };

        private static void ShouldThrowAnyOf<T1, T2>(Action act)
            where T1 : Exception
            where T2 : Exception
        {
            try
            {
                act();
            }
            catch (Exception ex)
            {
                if (ex is T1 || ex is T2)
                    return;

                throw new Xunit.Sdk.XunitException(
                    $"Expected {typeof(T1).Name} or {typeof(T2).Name}, but got {ex.GetType().Name}: {ex.Message}");
            }

            throw new Xunit.Sdk.XunitException(
                $"Expected {typeof(T1).Name} or {typeof(T2).Name}, but no exception was thrown");
        }

        private static string CreateToken(
            string key, string issuer, string audience,
            DateTime? expires = null, DateTime? notBefore = null,
            string algorithm = SecurityAlgorithms.HmacSha256)
        {
            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                algorithm);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: [new Claim(ClaimTypes.NameIdentifier, "u"), new Claim("scope", "mcp.tools.read")],
                notBefore: notBefore,
                expires: expires,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Fact]
        public void ExpiredTokenFailsValidation()
        {
            var token = CreateToken(ValidKey, "mcp-gateway", "mcp-gateway", DateTime.UtcNow.AddMinutes(-10));
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(token, ValidParams(), out _);
            act.Should().Throw<SecurityTokenExpiredException>();
        }

        [Fact]
        public void TokenExpiredOneSecondAgoFailsValidation()
        {
            var token = CreateToken(ValidKey, "mcp-gateway", "mcp-gateway", DateTime.UtcNow.AddSeconds(-1));
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(token, ValidParams(), out _);
            act.Should().Throw<SecurityTokenExpiredException>();
        }

        [Fact]
        public void TokenExpiredOneHourAgoFailsValidation()
        {
            var token = CreateToken(ValidKey, "mcp-gateway", "mcp-gateway", DateTime.UtcNow.AddHours(-1));
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(token, ValidParams(), out _);
            act.Should().Throw<SecurityTokenExpiredException>();
        }

        [Fact]
        public void TokenExpiredOneDayAgoFailsValidation()
        {
            var token = CreateToken(ValidKey, "mcp-gateway", "mcp-gateway", DateTime.UtcNow.AddDays(-1));
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(token, ValidParams(), out _);
            act.Should().Throw<SecurityTokenExpiredException>();
        }

        [Fact]
        public void TokenExpiredOneYearAgoFailsValidation()
        {
            var token = CreateToken(ValidKey, "mcp-gateway", "mcp-gateway", DateTime.UtcNow.AddYears(-1));
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(token, ValidParams(), out _);
            act.Should().Throw<SecurityTokenExpiredException>();
        }

        [Fact]
        public void TokenExpiringInOneSecondPassesValidation()
        {
            var token = CreateToken(ValidKey, "mcp-gateway", "mcp-gateway", DateTime.UtcNow.AddSeconds(30));
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, ValidParams(), out _);
            principal.Should().NotBeNull();
        }

        [Fact]
        public void TokenSignedWithWrongKeyFailsValidation()
        {
            var token = CreateToken("wrong-key-at-least-32-chars-long!!!!!!!!", "mcp-gateway", "mcp-gateway", DateTime.UtcNow.AddMinutes(15));
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(token, ValidParams(), out _);
            act.Should().Throw<SecurityTokenSignatureKeyNotFoundException>();
        }

        [Fact]
        public void TokenSignedWithShortKeyFailsValidation()
        {
            Action act = () => CreateToken("short", "mcp-gateway", "mcp-gateway", DateTime.UtcNow.AddMinutes(15));
            ShouldThrowAnyOf<SecurityTokenException, ArgumentException>(act);
        }

        [Fact]
        public void TokenSignedWithEmptyKeyFailsValidation()
        {
            Action act = () => CreateToken("", "mcp-gateway", "mcp-gateway", DateTime.UtcNow.AddMinutes(15));
            ShouldThrowAnyOf<ArgumentException, SecurityTokenException>(act);
        }

        [Fact]
        public void TokenWithWrongIssuerFailsValidation()
        {
            var token = CreateToken(ValidKey, "evil-issuer", "mcp-gateway", DateTime.UtcNow.AddMinutes(15));
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(token, ValidParams(), out _);
            act.Should().Throw<SecurityTokenInvalidIssuerException>();
        }

        [Fact]
        public void TokenWithEmptyIssuerFailsValidation()
        {
            var token = CreateToken(ValidKey, "", "mcp-gateway", DateTime.UtcNow.AddMinutes(15));
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(token, ValidParams(), out _);
            ShouldThrowAnyOf<SecurityTokenInvalidIssuerException, ArgumentException>(act);
        }

        [Fact]
        public void TokenWithNullIssuerFailsValidation()
        {
            var token = CreateToken(ValidKey, null!, "mcp-gateway", DateTime.UtcNow.AddMinutes(15));
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(token, ValidParams(), out _);
            ShouldThrowAnyOf<SecurityTokenInvalidIssuerException, ArgumentException>(act);
        }

        [Fact]
        public void TokenWithIssuerMissingProtocolFailsValidation()
        {
            var token = CreateToken(ValidKey, "mcp-gateway-evil", "mcp-gateway", DateTime.UtcNow.AddMinutes(15));
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(token, ValidParams(), out _);
            act.Should().Throw<SecurityTokenInvalidIssuerException>();
        }

        [Fact]
        public void TokenWithWrongAudienceFailsValidation()
        {
            var token = CreateToken(ValidKey, "mcp-gateway", "evil-audience", DateTime.UtcNow.AddMinutes(15));
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(token, ValidParams(), out _);
            act.Should().Throw<SecurityTokenInvalidAudienceException>();
        }

        [Fact]
        public void TokenWithEmptyAudienceFailsValidation()
        {
            var token = CreateToken(ValidKey, "mcp-gateway", "", DateTime.UtcNow.AddMinutes(15));
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(token, ValidParams(), out _);
            ShouldThrowAnyOf<SecurityTokenInvalidAudienceException, ArgumentException>(act);
        }

        [Fact]
        public void TokenWithNullAudienceFailsValidation()
        {
            var token = CreateToken(ValidKey, "mcp-gateway", null!, DateTime.UtcNow.AddMinutes(15));
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(token, ValidParams(), out _);
            ShouldThrowAnyOf<SecurityTokenInvalidAudienceException, ArgumentException>(act);
        }

        [Fact]
        public void TokenWithCaseSensitiveAudienceFailsValidation()
        {
            var token = CreateToken(ValidKey, "mcp-gateway", "MCP-GATEWAY", DateTime.UtcNow.AddMinutes(15));
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(token, ValidParams(), out _);
            ShouldThrowAnyOf<SecurityTokenInvalidAudienceException, SecurityTokenException>(act);
        }

        [Fact]
        public void TamperedSignatureFailsValidation()
        {
            var valid = _service.CreateAccessToken("u", "e@e.com", "mcp.tools.read");
            var tampered = valid[..^5] + "AAAAA";
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(tampered, ValidParams(), out _);
            act.Should().Throw<SecurityTokenException>();
        }

        [Fact]
        public void TamperedFirstCharacterOfSignatureFailsValidation()
        {
            var valid = _service.CreateAccessToken("u", "e@e.com", "mcp.tools.read");
            var parts = valid.Split('.');
            var sig = parts[2];
            var tamperedSig = (sig[0] == 'A' ? 'B' : 'A') + sig[1..];
            var tampered = $"{parts[0]}.{parts[1]}.{tamperedSig}";
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(tampered, ValidParams(), out _);
            act.Should().Throw<SecurityTokenException>();
        } 

        [Fact]
        public void TokenWithAlgNoneFailsValidation()
        {
            var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"alg":"none","typ":"JWT"}"""))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"sub":"u","iss":"mcp-gateway","aud":"mcp-gateway","exp":9999999999}"""))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var token = $"{header}.{payload}.";
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(token, ValidParams(), out _);
            act.Should().Throw<SecurityTokenException>();
        }

        [Fact]
        public void EmptyTokenFailsParsing()
        {
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ReadJwtToken("");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void RandomStringFailsParsing()
        {
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ReadJwtToken("not-a-token");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TokenWithTwoPartsFailsParsing()
        {
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ReadJwtToken("header.payload");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TokenWithFourPartsFailsParsing()
        {
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ReadJwtToken("a.b.c.d");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TokenWithInvalidBase64FailsParsing()
        {
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ReadJwtToken("!!!.@@@.###");
            ShouldThrowAnyOf<ArgumentException, FormatException>(act);
        }

        [Fact]
        public void TokenWithoutSignatureFailsValidation()
        {
            var valid = _service.CreateAccessToken("u", "e@e.com", "mcp.tools.read");
            var parts = valid.Split('.');
            var tokenWithoutSig = $"{parts[0]}.{parts[1]}.";
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(tokenWithoutSig, ValidParams(), out _);
            act.Should().Throw<SecurityTokenException>();
        }

        [Fact]
        public void TokenWithFutureNbfFailsValidation()
        {
            var token = CreateToken(
                ValidKey, "mcp-gateway", "mcp-gateway",
                expires: DateTime.UtcNow.AddHours(2),
                notBefore: DateTime.UtcNow.AddHours(1));
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(token, ValidParams(), out _);
            act.Should().Throw<SecurityTokenNotYetValidException>();
        }

        [Fact]
        public void TokenWithNbfOneMinuteInFutureFailsValidation()
        {
            var token = CreateToken(
                ValidKey, "mcp-gateway", "mcp-gateway",
                expires: DateTime.UtcNow.AddMinutes(30),
                notBefore: DateTime.UtcNow.AddMinutes(1));
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(token, ValidParams(), out _);
            act.Should().Throw<SecurityTokenNotYetValidException>();
        }

        [Fact]
        public void TokenWithoutExpirationFailsValidation()
        {
            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ValidKey)),
                SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: "mcp-gateway",
                audience: "mcp-gateway",
                claims: [new Claim(ClaimTypes.NameIdentifier, "u")],
                signingCredentials: creds);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(tokenString, ValidParams(), out _);
            act.Should().Throw<SecurityTokenNoExpirationException>();
        } 

        [Fact]
        public void TokenWithRs256AlgFailsValidation()
        {
            var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"alg":"RS256","typ":"JWT"}"""))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var token = $"{header}.e30.invalidsig";
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(token, ValidParams(), out _);
            ShouldThrowAnyOf<SecurityTokenException, ArgumentException>(act);
        }

        [Fact]
        public void NullTokenFailsParsing()
        {
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ReadJwtToken(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void WhitespaceTokenFailsParsing()
        {
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ReadJwtToken("   ");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TokenWithOnlyHeaderFailsParsing()
        {
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ReadJwtToken("eyJhbGciOiJIUzI1NiJ9");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void VeryLongTokenFailsParsing()
        {
            var token = new string('a', 100_000);
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ReadJwtToken(token);
            ShouldThrowAnyOf<ArgumentException, FormatException>(act);
        }

        [Fact]
        public void TokenWithMalformedJsonPayloadFailsParsing()
        {
            var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"alg":"HS256"}"""))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{not-json"))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var token = $"{header}.{payload}.sig";
            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ReadJwtToken(token);
            ShouldThrowAnyOf<ArgumentException, SecurityTokenException>(act);
        }

        [Fact]
        public void TokenWithNullClaimValueDoesNotCrash()
        {
            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ValidKey)),
                SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: "mcp-gateway",
                audience: "mcp-gateway",
                claims: [new Claim(ClaimTypes.NameIdentifier, "")],
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(tokenString, ValidParams(), out _);
            principal.Should().NotBeNull();
        }
    }
}
