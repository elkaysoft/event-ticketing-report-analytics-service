using ETS.Domain.AppConfig;
using ETS.Domain.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace ETS.Infrastructure.Authentication
{
    public class JwtBearerOptionsSetup : IConfigureNamedOptions<JwtBearerOptions>
    {
        private readonly AuthenticationOptions _authenticationOptions;

        public JwtBearerOptionsSetup(IOptions<AuthenticationOptions> authenticationOptions)
        {
            _authenticationOptions = authenticationOptions.Value;
        }

        public void Configure(JwtBearerOptions options)
        {
            options.Audience = _authenticationOptions.Audience;
            options.MetadataAddress = _authenticationOptions.MetadataUrl;
            options.RequireHttpsMetadata = _authenticationOptions.RequireHttpsMetadata;
            options.SaveToken = true;

            var key = Helpers.CreateRsaSecurityKey(_authenticationOptions.IssuerKey);

            options.TokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKey = key,
                SaveSigninToken = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ValidIssuer = _authenticationOptions.Issuer,
                ValidAudience = _authenticationOptions.Audience,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            options.Events = CreateJwtBearerEvents();
        }

        public void Configure(string? name, JwtBearerOptions options)
        {
            Configure(options);
        }


        /// <summary>
        /// Creates standard JWT bearer events for logging authentication outcomes
        /// </summary>
        /// <returns></returns>
        private static JwtBearerEvents CreateJwtBearerEvents() => new()
        {
            OnAuthenticationFailed = c =>
            {
                c.NoResult();
                c.HttpContext.Items["AuthFailure"] = c.Exception;
                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                context.HandleResponse(); // suppress ASP.NET default response

                if (context.Response.HasStarted) return Task.CompletedTask;

                var exception = context.HttpContext.Items["AuthFailure"] as Exception;

                var message = exception switch
                {
                    SecurityTokenExpiredException => "Token has expired. Please re-authenticate.",
                    SecurityTokenInvalidSignatureException => "Token signature is invalid.",
                    SecurityTokenInvalidIssuerException => "Token issuer is invalid.",
                    SecurityTokenInvalidAudienceException => "Token audience is invalid.",
                    not null => "Token is invalid.",
                    null => "Unauthorized. Token is missing."
                };

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync(JsonSerializer.Serialize(new { message }));
            },

            OnForbidden = context =>
            {
                if (context.Response.HasStarted) return Task.CompletedTask;

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new { message = "You are not allowed to access this resource." });
                return context.Response.WriteAsync(result);
            }
        };
    }

}
