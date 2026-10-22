namespace Gateway.Api.Services
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;

    using Gateway.Api.Data;
    using Gateway.Core.Entities;
 
    public sealed class AuthService(
        UserDbContext db,
        ITokenService tokenService,
        IPasswordHasher<User> passwordHasher,
        ILogger<AuthService> logger) : IAuthService
    {
        private static readonly Action<ILogger, string, Exception?> _logUserRegistered =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(1, nameof(Register)),
                "User {Email} registered");

        private static readonly Action<ILogger, string, Exception?> _logUserLoggedIn =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(2, nameof(Login)),
                "User {Email} logged in");

        private static readonly Action<ILogger, string, Exception?> _logLoginFailedUserNotFound =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(3, "LoginFailedUserNotFound"),
                "Login failed: user {Email} not found");

        private static readonly Action<ILogger, string, Exception?> _logLoginFailedWrongPassword =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(4, "LoginFailedWrongPassword"),
                "Login failed: wrong password for {Email}");

        private static readonly Action<ILogger, string, Exception?> _logRegistrationFailedEmailExists =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(5, "RegistrationFailedEmailExists"),
                "Registration failed: {Email} already exists");

        private static readonly Action<ILogger, string, Exception?> _logTokenGenerationFailed =
            LoggerMessage.Define<string>(
                LogLevel.Error,
                new EventId(6, "TokenGenerationFailed"),
                "Failed to generate token for {Email}");

        public AuthUserResponse? Register(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || request.Password.Length < 6)
                return null;

            if (db.Users.Any(u => u.Email == request.Email))
            {
                _logRegistrationFailedEmailExists(logger, request.Email, null);
                return null;
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email
            };
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

            db.Users.Add(user);
            db.SaveChanges();

            _logUserRegistered(logger, request.Email, null);
            return new AuthUserResponse(user.Id.ToString(), user.Email);
        }

        public AuthUserResponse? Login(LoginRequest request)
        {
            var user = db.Users.AsNoTracking().FirstOrDefault(u => u.Email == request.Email);
            if (user is null)
            {
                _logLoginFailedUserNotFound(logger, request.Email, null);
                return null;
            }

            var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                _logLoginFailedWrongPassword(logger, request.Email, null);
                return null;
            }

            _logUserLoggedIn(logger, request.Email, null);
            return new AuthUserResponse(user.Id.ToString(), user.Email);
        } 

        public string? GenerateTokenFor(AuthUserResponse user, params string[] scopes)
        {
            try
            {
                return tokenService.CreateAccessToken(user.SubjectId, user.Email, scopes);
            }
            catch (Exception ex)
            {
                _logTokenGenerationFailed(logger, user.Email, ex);
                return null;
            } 
        }
    }
}