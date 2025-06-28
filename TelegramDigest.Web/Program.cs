using DotNetEnv;
using Microsoft.Extensions.Options;
using TelegramDigest.Backend;
using TelegramDigest.Web.Options;
using TelegramDigest.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Setup options
Env.TraversePath().Load("default_options.env");
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddOptions<WebDeploymentOptions>().Bind(builder.Configuration).ValidateOnStart();

// Backend configuration
builder.AddBackend();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddScoped<BackendClient>();

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
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapControllers();

await app.Services.UseBackend();

app.Run();
