using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;

namespace Ahead.Idp;

public static class CertService
{
    /// <summary>
    /// See original src:
    /// https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/web-apps-apis/confidential-client-assertions#crafting-the-assertion
    /// </summary>
    public static string GetSignedClientAssertion(X509Certificate2 certificate, string tenantId, string clientId)
    {
        // single tenant
        var tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

        // common

        // no need to add exp, nbf as JsonWebTokenHandler will add them by default.
        var claims = new Dictionary<string, object>()
        {
            { "aud", tokenEndpoint },
            { "iss", clientId },
            { "jti", Guid.NewGuid().ToString() },
            { "sub", clientId }
        };

        var securityTokenDescriptor = new SecurityTokenDescriptor
        {
            Claims = claims,
            SigningCredentials = new X509SigningCredentials(certificate)
        };

        var handler = new JsonWebTokenHandler();
        var signedClientAssertion = handler.CreateToken(securityTokenDescriptor);

        return signedClientAssertion;
    }
}
