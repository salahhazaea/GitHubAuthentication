using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

public static class RegisterExtension
{
    public static IServiceCollection AddOAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = "GitHub";
        })
        .AddCookie()
        .AddOAuth("GitHub", Options =>
        {
            Options.ClientId = configuration["GitHub:ClientId"];
            Options.ClientSecret = configuration["GitHub:ClientSecret"];
            Options.AuthorizationEndpoint = configuration["GitHub:AuthorizationEndpoint"];
            Options.TokenEndpoint = configuration["GitHub:TokenEndpoint"];
            Options.UserInformationEndpoint = configuration["GitHub:UserInformationEndpoint"];
            Options.CallbackPath = new PathString("/ReturnFromGitHub");

            Options.SaveTokens = true;
            Options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
            Options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
            Options.ClaimActions.MapJsonKey("urn:github:login", "login");
            Options.ClaimActions.MapJsonKey("urn:github:url", "html_url");
            Options.ClaimActions.MapJsonKey("urn:github:avatar", "avatar_url");
            Options.CorrelationCookie.SameSite = SameSiteMode.Lax;

            Options.Events = new OAuthEvents
            {
                OnCreatingTicket = async context =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                    var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                    response.EnsureSuccessStatusCode();
                    var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    context.RunClaimActions(json.RootElement);
                },
                OnRemoteFailure = context =>
                {
                    Console.WriteLine(context.Failure?.Message);
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }
}