# Use client assertions in OpenID Connect and ASP.NET Core

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