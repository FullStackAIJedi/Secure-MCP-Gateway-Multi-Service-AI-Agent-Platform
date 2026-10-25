namespace Gateway.IntegrationTests
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;

    using Microsoft.IdentityModel.Tokens;
    using Microsoft.Extensions.Options;

    using Gateway.Api.Configuration;
    using Gateway.Api.Services;

    using Xunit;
    using FluentAssertions;

    public sealed class TokenServiceTests
    {
        private static readonly JwtOptions DefaultOptions = new()
        {
            Key = "super-secret-key-at-least-32-chars-long!!",
            Issuer = "mcp-gateway",
            Audience = "mcp-gateway",
            AccessTokenMinutes = 15
        };

        private readonly TokenService _service =
            new(Microsoft.Extensions.Options.Options.Create(DefaultOptions));

        private static JwtSecurityToken Parse(string token) =>
            new JwtSecurityTokenHandler().ReadJwtToken(token);


        [Fact]
        public void CreateAccessTokenReturnsNonEmptyString()
        {
            var token = _service.CreateAccessToken("subject-1", "test@test.com", "mcp.tools.read");

            token.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void CreateAccessTokenIsThreePartsJwt()
        {
            var token = _service.CreateAccessToken("subject-1", "test@test.com", "scope");

            token.Split('.').Should().HaveCount(3);
        }

        [Fact]
        public void CreateAccessTokenWithNoScopesStillWorks()
        {
            var token = _service.CreateAccessToken("subject-1", "test@test.com");

            token.Should().NotBeNullOrWhiteSpace();
            var parsed = Parse(token);
            parsed.Claims.Should().Contain(c => c.Type == "scope");
        }

        [Fact]
        public void CreateAccessTokenWithEmptyScopesStillWorks()
        {
            var token = _service.CreateAccessToken("subject-1", "test@test.com", []);

            token.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void CreateAccessTokenContainsNameIdentifier()
        {
            var token = _service.CreateAccessToken("subject-1", "test@test.com", "scope");
            var parsed = Parse(token);

            parsed.Claims.Should().Contain(c =>
                c.Type == ClaimTypes.NameIdentifier && c.Value == "subject-1");
        }

        [Fact]
        public void CreateAccessTokenContainsEmail()
        {
            var token = _service.CreateAccessToken("subject-1", "test@test.com", "scope");
            var parsed = Parse(token);

            parsed.Claims.Should().Contain(c =>
                c.Type == ClaimTypes.Email && c.Value == "test@test.com");
        }

        [Fact]
        public void CreateAccessTokenContainsSingleScope()
        {
            var token = _service.CreateAccessToken("s", "e@e.com", "mcp.tools.read");
            var parsed = Parse(token);

            parsed.Claims.Should().Contain(c =>
                c.Type == "scope" && c.Value == "mcp.tools.read");
        }

        [Fact]
        public void CreateAccessTokenCombinesMultipleScopesWithSpaces()
        {
            var token = _service.CreateAccessToken("s", "e@e.com",
                "mcp.tools.read", "mcp.tools.write", "mcp.tools.admin");
            var parsed = Parse(token);

            var scopeClaim = parsed.Claims.Single(c => c.Type == "scope");
            scopeClaim.Value.Should().Be("mcp.tools.read mcp.tools.write mcp.tools.admin");
        }

        [Fact]
        public void CreateAccessTokenWithSingleScopeDoesNotHaveSpaces()
        {
            var token = _service.CreateAccessToken("s", "e@e.com", "mcp.tools.read");
            var parsed = Parse(token);

            var scopeClaim = parsed.Claims.Single(c => c.Type == "scope");
            scopeClaim.Value.Should().Be("mcp.tools.read");
            scopeClaim.Value.Should().NotContain(" ");
        }

        [Fact]
        public void CreateAccessTokenHasCorrectIssuer()
        {
            var token = _service.CreateAccessToken("s", "e@e.com", "scope");
            var parsed = Parse(token);

            parsed.Issuer.Should().Be("mcp-gateway");
        }

        [Fact]
        public void CreateAccessTokenHasCorrectAudience()
        {
            var token = _service.CreateAccessToken("s", "e@e.com", "scope");
            var parsed = Parse(token);

            parsed.Audiences.Should().ContainSingle().Which.Should().Be("mcp-gateway");
        }

        [Fact]
        public void CreateAccessTokenWithCustomIssuerUsesIt()
        {
            var opts = new JwtOptions
            {
                Key = DefaultOptions.Key,
                Issuer = "custom-issuer",
                Audience = "custom-audience",
                AccessTokenMinutes = 15
            };
            var service = new TokenService(Microsoft.Extensions.Options.Options.Create(opts));

            var token = service.CreateAccessToken("s", "e@e.com", "scope");
            var parsed = Parse(token);

            parsed.Issuer.Should().Be("custom-issuer");
            parsed.Audiences.Should().Contain("custom-audience");
        }

        [Fact]
        public void CreateAccessTokenExpiresIn15MinutesByDefault()
        {
            var before = DateTime.UtcNow.AddMinutes(15);
            var token = _service.CreateAccessToken("s", "e@e.com", "scope");
            var after = DateTime.UtcNow.AddMinutes(15);

            var parsed = Parse(token);
            parsed.ValidTo.Should().BeOnOrAfter(before.AddSeconds(-5));
            parsed.ValidTo.Should().BeOnOrBefore(after.AddSeconds(5));
        }

        [Fact]
        public void CreateAccessTokenWithCustomExpirationUsesIt()
        {
            var opts = new JwtOptions
            {
                Key = DefaultOptions.Key,
                Issuer = DefaultOptions.Issuer,
                Audience = DefaultOptions.Audience,
                AccessTokenMinutes = 60
            };
            var service = new TokenService(Microsoft.Extensions.Options.Options.Create(opts));

            var before = DateTime.UtcNow.AddMinutes(60);
            var token = service.CreateAccessToken("s", "e@e.com", "scope");
            var after = DateTime.UtcNow.AddMinutes(60);

            var parsed = Parse(token);
            parsed.ValidTo.Should().BeOnOrAfter(before.AddSeconds(-5));
            parsed.ValidTo.Should().BeOnOrBefore(after.AddSeconds(5));
        }

        [Fact]
        public void CreateAccessTokenWithOneMinuteExpiration()
        {
            var opts = new JwtOptions
            {
                Key = DefaultOptions.Key,
                Issuer = DefaultOptions.Issuer,
                Audience = DefaultOptions.Audience,
                AccessTokenMinutes = 1
            };
            var service = new TokenService(Microsoft.Extensions.Options.Options.Create(opts));

            var before = DateTime.UtcNow.AddMinutes(1);
            var token = service.CreateAccessToken("s", "e@e.com", "scope");
            var after = DateTime.UtcNow.AddMinutes(1);

            var parsed = Parse(token);
            parsed.ValidTo.Should().BeOnOrAfter(before.AddSeconds(-5));
            parsed.ValidTo.Should().BeOnOrBefore(after.AddSeconds(5));
        }

        [Fact]
        public void CreateAccessTokenIsSignedWithHs256()
        {
            var token = _service.CreateAccessToken("s", "e@e.com", "scope");
            var parsed = Parse(token);

            parsed.Header.Alg.Should().Be(SecurityAlgorithms.HmacSha256);
        }

        [Fact]
        public void CreateAccessTokenSignatureValidatesWithSameKey()
        {
            var token = _service.CreateAccessToken("s", "e@e.com", "scope");

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "mcp-gateway",
                ValidAudience = "mcp-gateway",
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(DefaultOptions.Key))
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, validationParams, out var validatedToken);

            validatedToken.Should().NotBeNull();
            principal.Should().NotBeNull();
        }

        [Fact]
        public void CreateAccessTokenSignatureFailsWithWrongKey()
        {
            var token = _service.CreateAccessToken("s", "e@e.com", "scope");

            var wrongKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("completely-different-key-at-least-32-chars-long"));

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "mcp-gateway",
                ValidAudience = "mcp-gateway",
                IssuerSigningKey = wrongKey
            };

            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(token, validationParams, out _);

            act.Should().Throw<SecurityTokenSignatureKeyNotFoundException>();
        }

        [Fact]
        public void CreateAccessTokenSignatureFailsWithWrongIssuer()
        {
            var token = _service.CreateAccessToken("s", "e@e.com", "scope");

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "wrong-issuer",
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(DefaultOptions.Key))
            };

            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(token, validationParams, out _);

            act.Should().Throw<SecurityTokenInvalidIssuerException>();
        }

        [Fact]
        public void CreateAccessTokenSignatureFailsWithWrongAudience()
        {
            var token = _service.CreateAccessToken("s", "e@e.com", "scope");

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidAudience = "wrong-audience",
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(DefaultOptions.Key))
            };

            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(token, validationParams, out _);

            act.Should().Throw<SecurityTokenInvalidAudienceException>();
        }

        [Fact]
        public void TamperedTokenFailsValidation()
        {
            var token = _service.CreateAccessToken("s", "e@e.com", "scope");
            var tampered = token[..^5] + "AAAAA";

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "mcp-gateway",
                ValidAudience = "mcp-gateway",
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(DefaultOptions.Key))
            };

            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(tampered, validationParams, out _);

            act.Should().Throw<SecurityTokenException>();
        }

        [Fact]
        public void TokenWithModifiedPayloadFailsValidation()
        {
            var token = _service.CreateAccessToken("s", "e@e.com", "scope");
            var parts = token.Split('.');

            var payloadBytes = Convert.FromBase64String(PadBase64(parts[1]));
            var payloadJson = Encoding.UTF8.GetString(payloadBytes);
            var modifiedJson = payloadJson.Replace("e@e.com", "hacker@hack.com");
            var modifiedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(modifiedJson))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            var tampered = $"{parts[0]}.{modifiedPayload}.{parts[2]}";

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(DefaultOptions.Key))
            };

            var handler = new JwtSecurityTokenHandler();
            Action act = () => handler.ValidateToken(tampered, validationParams, out _);

            act.Should().Throw<SecurityTokenException>();
        }

        private static string PadBase64(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: return s + "==";
                case 3: return s + "=";
                default: return s;
            }
        }

        [Fact]
        public void TwoTokensForSameUserHaveSameClaimsButDifferentExp()
        {
            var t1 = _service.CreateAccessToken("s", "e@e.com", "scope");
            var t2 = _service.CreateAccessToken("s", "e@e.com", "scope");

            var p1 = Parse(t1);
            var p2 = Parse(t2);

            p1.Claims.Where(c => c.Type == "scope").Single().Value
                .Should().Be(p2.Claims.Where(c => c.Type == "scope").Single().Value);
        } 

        [Fact]
        public void CreateAccessTokenWithSpecialCharactersInEmail()
        {
            var token = _service.CreateAccessToken("s", "user+tag@sub.domain.com", "scope");
            var parsed = Parse(token);

            parsed.Claims.Should().Contain(c =>
                c.Type == ClaimTypes.Email && c.Value == "user+tag@sub.domain.com");
        }

        [Fact]
        public void CreateAccessTokenWithUnicodeInSubject()
        {
            var token = _service.CreateAccessToken("потребител-1", "e@e.com", "scope");
            var parsed = Parse(token);

            parsed.Claims.Should().Contain(c =>
                c.Type == ClaimTypes.NameIdentifier && c.Value == "потребител-1");
        }

        [Fact]
        public void CreateAccessTokenWithVeryLongSubject()
        {
            var longSubject = new string('a', 1000);
            var token = _service.CreateAccessToken(longSubject, "e@e.com", "scope");

            token.Should().NotBeNullOrWhiteSpace();
            Parse(token).Claims.Should().Contain(c =>
                c.Type == ClaimTypes.NameIdentifier && c.Value == longSubject);
        }

        [Fact]
        public void CreateAccessTokenWithManyScopes()
        {
            var scopes = Enumerable.Range(1, 20)
                .Select(i => $"mcp.tools.scope{i}")
                .ToArray();

            var token = _service.CreateAccessToken("s", "e@e.com", scopes);
            var parsed = Parse(token);

            var scopeClaim = parsed.Claims.Single(c => c.Type == "scope");
            scopeClaim.Value.Split(' ').Should().HaveCount(20);
        }

        [Fact]
        public void CreateAccessTokenWithDuplicateScopes()
        {
            var token = _service.CreateAccessToken("s", "e@e.com",
                "mcp.tools.read", "mcp.tools.read");
            var parsed = Parse(token);

            var scopeClaim = parsed.Claims.Single(c => c.Type == "scope");
            scopeClaim.Value.Should().Be("mcp.tools.read mcp.tools.read");
        }
    }
}
