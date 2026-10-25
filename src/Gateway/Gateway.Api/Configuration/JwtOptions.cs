namespace Gateway.Api.Configuration
{
    using System.ComponentModel.DataAnnotations; 

    public sealed class JwtOptions
    {
        public const string SectionName = "Jwt";

        [Required, MinLength(32)]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Issuer { get; set; } = string.Empty;

        [Required]
        public string Audience { get; set; } = string.Empty;

        [Range(1, 60)]
        public int AccessTokenMinutes { get; set; } = 15;
    }
}
