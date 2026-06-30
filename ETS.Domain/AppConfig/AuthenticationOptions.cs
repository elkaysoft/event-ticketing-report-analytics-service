namespace ETS.Domain.AppConfig
{
    public sealed class AuthenticationOptions
    {
        public string Audience { get; set; } = string.Empty;
        public string MetadataUrl { get; set; } = string.Empty;
        public bool RequireHttpsMetadata { get; set; }
        public string Issuer { get; set; } = string.Empty;
        public string IssuerKey { get; set; } = string.Empty;
        public int TokenExpiryInSeconds { get; set; }
        public int RefreshTokenExpiryInSeconds { get; set; }
    }
}
