namespace Gateway.Api.Services
{  
    public interface ITokenService
    {
        string CreateAccessToken(string subjectId, string email, params string[] scopes);
    }
}
