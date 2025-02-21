using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;

namespace OidcClientAssertion;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();

        var clientAssertion = CertService.GetSignedClientAssertion(
            X509CertificateLoader.LoadPkcs12FromFile("cert_rsa512.pfx", "1234"),
            builder.Configuration["AzureAd:TenantId"]!,
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
                //oidcOptions.ClientSecret = builder.Configuration["AzureAd:ClientSecret"];
                oidcOptions.ResponseType = OpenIdConnectResponseType.Code;
                oidcOptions.MapInboundClaims = false;
                oidcOptions.SaveTokens = true;
                oidcOptions.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
                oidcOptions.TokenValidationParameters.RoleClaimType = "role";

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

        var requireAuthPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(requireAuthPolicy);



        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapRazorPages()
           .WithStaticAssets();

        app.Run();
    }
}
