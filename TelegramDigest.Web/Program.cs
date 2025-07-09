using System.Diagnostics;
using DotNetEnv;
using Microsoft.Extensions.Options;
using TelegramDigest.Backend;
using TelegramDigest.Backend.Infrastructure;
using TelegramDigest.Web.Infrastructure.Auth;
using TelegramDigest.Web.Options;
using TelegramDigest.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Setup options
Env.TraversePath().Load("default_options.env");
builder.Configuration.AddEnvironmentVariables();
builder
    .Services.AddOptions<WebDeploymentOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder
    .Services.AddOptions<AuthOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Backend configuration
builder.AddBackendCustom();

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BackendClient>();
builder.Services.AddSession();

builder.Services.AddAuthenticationCustom();

// Pass authentication configuration to the backend
builder.Services.AddTransient(provider =>
{
    var authOptions = provider.GetRequiredService<IOptions<AuthOptions>>().Value;
    return new BackendAuthConfiguration(
        authOptions.ReverseProxyHeaderId,
        authOptions.Mode switch
        {
            AuthMode.SingleUser => AuthenticationMode.SingleUser,
            AuthMode.ReverseProxy => AuthenticationMode.ReverseProxy,
            AuthMode.OpenIdConnect => AuthenticationMode.OpenIdConnect,
            _ => throw new UnreachableException($"Unknown auth mode: {authOptions.Mode}"),
        },
        authOptions.SingleUserId
    );
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

var deploymentOptions = app.Services.GetRequiredService<IOptions<WebDeploymentOptions>>().Value;

app.UsePathBase(deploymentOptions.BasePath);
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapControllers();

await app.UseBackendCustom();

app.Run();
