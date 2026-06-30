using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace ETS.Domain.Common
{
    public static class Helpers
    {
        public static string SanitizeForLogging(this string? input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return string.Create(input.Length, input, static (span, s) =>
            {
                for (var i = 0; i < s.Length; i++)
                {
                    var c = s[i];
                    if (c is '\r' or '\n' or '\t' or '\u2028' or '\u2029')
                    {
                        span[i] = ' ';
                    }
                    else if (char.IsControl(c))
                    {
                        span[i] = ' ';
                    }
                    else
                    {
                        span[i] = c;
                    }
                }
            });
        }

        public static RsaSecurityKey CreateRsaSecurityKey(string issuerKey)
        {
            var rsa = RSA.Create();

            if (issuerKey.TrimStart().StartsWith("-----BEGIN", StringComparison.OrdinalIgnoreCase))
            {
                // PEM format - replace literal \n escapes from JSON config with actual newlines
                var pemKey = issuerKey.Replace("\\n", "\n");
                rsa.ImportFromPem(pemKey);
            }
            else if (issuerKey.TrimStart().StartsWith("<", StringComparison.OrdinalIgnoreCase))
            {
                // XML format (legacy keys)
                rsa.FromXmlString(issuerKey);
            }
            else
            {
                throw new InvalidOperationException(
                    "Unsupported RSA key format. The IssuerKey must be in PEM (-----BEGIN PUBLIC KEY-----) or XML (<RSAKeyValue>) format.");
            }

            return new RsaSecurityKey(rsa);
        }



    }
}
