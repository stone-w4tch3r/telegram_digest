using DotNetEnv;
using Microsoft.Extensions.Options;
using TelegramDigest.Backend;
using TelegramDigest.Backend.Infrastructure;
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
    .Services.AddOptions<AuthenticationOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Backend configuration
builder.AddBackend();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BackendClient>();

// Pass authentication configuration to the backend
builder.Services.AddTransient(provider =>
{
    var authOptions = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
    return new BackendAuthenticationConfiguration(
        authOptions.ProxyHeaderId,
        authOptions switch
        {
            _ when authOptions.SingleUserMode => AuthenticationMode.SingleUser,
            _ when authOptions.ProxyHeaderId != null => AuthenticationMode.ReverseProxy,
            _ when authOptions.Authority != null => AuthenticationMode.OpenIdConnect,
            _ => throw new InvalidOperationException(
                $"{nameof(AuthenticationOptions)} is misconfigured"
            ),
        }
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
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
app.MapRazorPages();
app.MapControllers();

await app.Services.UseBackend();

app.Run();
