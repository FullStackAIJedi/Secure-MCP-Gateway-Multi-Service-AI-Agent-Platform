namespace Gateway.Api
{  
    public sealed record RegisterRequest(string Email, string Password); 
    public sealed record LoginRequest(string Email, string Password); 
    public sealed record AuthUserResponse(string SubjectId, string Email); 
    public sealed record LoginResponse(AuthUserResponse User);
}
