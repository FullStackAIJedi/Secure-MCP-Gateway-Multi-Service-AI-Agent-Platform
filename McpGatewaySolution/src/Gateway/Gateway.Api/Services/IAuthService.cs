namespace Gateway.Api.Services
{  
    public interface IAuthService
    { 
        AuthUserResponse? Register(RegisterRequest request);
        AuthUserResponse? Login(LoginRequest request);
        string? GenerateTokenFor(AuthUserResponse user, params string[] scopes);
    }
}
