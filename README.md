# Use client assertions in OpenID Connect and ASP.NET Core

[![.NET](https://github.com/damienbod/oidc-client-assertion/actions/workflows/dotnet.yml/badge.svg)](https://github.com/damienbod/oidc-client-assertion/actions/workflows/dotnet.yml)

Blog: [Use client assertions in OpenID Connect and ASP.NET Core](https://damienbod.com/2025/02/24/use-client-assertions-in-openid-connect-and-asp-net-core/)

Client assertions is a method of client authentication which can be used in OpenID Connect. This provides an alternative to client secrets. This approach enhances security by using signed tokens (JWTs) to authenticate clients during the token request process or the OAuth PAR request. In ASP.NET Core, client assertions is not supported per default, a small implementation is required.

![flow](https://github.com/damienbod/oidc-client-assertion/blob/main/images/oidc-confidential-pkce-code-flow-assertion.drawio.png)


## Use an assertion in ASP.NET Core (token request):

when using OAuth PAR, the client assertion is set in the OnPushAuthorization event.

```csharp

// single tenant
var aud = $"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]!}/oauth2/v2.0/token";

var clientAssertion = CertService.GetSignedClientAssertion(
	X509CertificateLoader.LoadPkcs12FromFile("cert_rsa512.pfx", "1234"),
	aud,
	builder.Configuration["AzureAd:ClientId"]!);

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, oidcOptions =>
{
	oidcOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
	oidcOptions.Scope.Add(OpenIdConnectScope.OpenIdProfile);
	oidcOptions.Scope.Add("user.read");
	oidcOptions.Scope.Add(OpenIdConnectScope.OfflineAccess);
	oidcOptions.Authority = $"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/v2.0/";
	oidcOptions.ClientId = builder.Configuration["AzureAd:ClientId"];
		
	oidcOptions.ResponseType = OpenIdConnectResponseType.Code;
	oidcOptions.MapInboundClaims = false;
	oidcOptions.SaveTokens = true;
	oidcOptions.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
	oidcOptions.TokenValidationParameters.RoleClaimType = "role";

	//oidcOptions.ClientSecret = builder.Configuration["AzureAd:ClientSecret"];

	oidcOptions.Events = new OpenIdConnectEvents
	{
		// Add client_assertion            
		OnAuthorizationCodeReceived = context =>
		{
			context.TokenEndpointRequest!.ClientAssertion = clientAssertion;
			context.TokenEndpointRequest.ClientAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
			return Task.FromResult(0);
		}
		//OnPushAuthorization = context =>
		//{
		//    context.TokenEndpointRequest.ClientAssertion = clientAssertion;
		//    context.TokenEndpointRequest.ClientAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
		//    return Task.FromResult(0);
		//}
	};
});
```

## Links

https://datatracker.ietf.org/doc/html/rfc7521

https://datatracker.ietf.org/doc/html/rfc7523

https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/web-apps-apis/confidential-client-assertions

https://github.com/AzureAD/microsoft-identity-web/blob/2b8fbf0104d820bba8785c41b2ef9e6f801b5e73/src/Microsoft.Identity.Web.TokenAcquisition/MsAuth10AtPop.cs#L48

https://curity.io/resources/learn/jwt-assertion/

https://oauth.net/private-key-jwt/

https://github.com/AzureAD/microsoft-identity-web/wiki/Using-certificates

https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-oidc-web-authentication